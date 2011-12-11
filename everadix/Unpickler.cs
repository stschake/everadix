using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace everadix
{

    public abstract class PyObject
    {
        
    }

    public abstract class PyContainer : PyObject
    {
        public abstract void Append(PyObject item);
    }

    public class PyInt : PyObject
    {
        public int Value { get; set; }

        public PyInt(int value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class PyString : PyObject
    {
        public byte[] Data { get; set; }

        public string Value
        {
            get { return Encoding.ASCII.GetString(Data); }
        }

        public PyString(byte[] data)
        {
            Data = data;
        }

        public override string ToString()
        {
            if (Data.Length > 128 || Data.Count(b => char.IsControl((char)b)) > 3)
                return "binary data";
            return "'" + Value + "'";
        }
    }

    public class PyTuple : PyContainer
    {
        public List<PyObject> Items { get; private set; }
 
        public PyTuple(params PyObject[] items)
        {
            Items = new List<PyObject>(items);
        }

        public override void Append(PyObject item)
        {
            Items.Add(item);
        }
    }

    public class PyList : PyContainer
    {
        public List<PyObject> Items { get; private set; }

        public PyList(params PyObject[] items)
        {
            Items = new List<PyObject>(items);
        }

        public override void Append(PyObject item)
        {
            Items.Add(item);
        }
    }

    public class PyDict : PyObject
    {
        public Dictionary<PyObject, PyObject> Dict { get; private set; }
 
        public PyDict(params PyObject[] pairs)
        {
            Dict = new Dictionary<PyObject, PyObject>(pairs.Length / 2);
            for (int i = 0; i < pairs.Length/2; i++)
                Dict.Add(pairs[i*2], pairs[i*2+1]);
        }

        public PyObject this[string key]
        {
            get
            {
                return
                    Dict.Where(kvp => kvp.Key is PyString && (kvp.Key as PyString).Value == key)
                        .Select(kvp => kvp.Value).FirstOrDefault();
            }
        }
    }

    public class PyLong : PyObject
    {
        public long Value { get; private set; }

        public DateTime DateTime
        {
            get { return DateTime.FromFileTime(Value); }
        }
        
        public PyLong(long value)
        {
            Value = value;
        }

        public override string ToString()
        {
            if ((DateTime.Now - DateTime) < TimeSpan.FromDays(5 * 365))
                return DateTime.ToString();
            return Value.ToString();
        }
    }

    public class PyBool : PyObject
    {
        public bool Value { get; private set; }

        public PyBool(bool value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    /// <summary>
    /// exclusively used for the unpickling
    /// </summary>
    public class PyMark : PyObject
    {
        public override string ToString()
        {
            return "--- mark ---";
        }
    }

    /// <summary>
    /// based on the implementation of the IronPython project
    /// under the Apache License 2.0, former Microsoft Open Source
    /// </summary>
    public class Unpickler
    {
        private readonly PyObject _mark = new PyMark();
        private List<PyObject> _stack;
        public int Protocol { get; set; }
        public PyObject Result { get; set; }

        public Unpickler(Stream source)
        {
            Load(new BinaryReader(source));
        }

        public static PyObject Load(Stream source)
        {
            var unpickler = new Unpickler(source);
            return unpickler.Result;
        }

        public static PyObject Load(byte[] data)
        {
            return Load(new MemoryStream(data));
        }

        private int FindMark()
        {
            for (int i = _stack.Count - 1; i >= 0; i--)
                if (ReferenceEquals(_stack.ElementAt(i), _mark))
                    return i;
            throw new Exception("Mark not found");
        }

        private void Load(BinaryReader reader)
        {
            try
            {
                var op = (Opcode) reader.ReadByte();
                if (op != Opcode.Proto)
                    throw new InvalidDataException("Expected Protocol opcode");
                Protocol = reader.ReadByte();
                if (Protocol != 2)
                    throw new InvalidDataException("Only protocol version 2 is supported");

                _stack = new List<PyObject>(15);
                var memo = new Dictionary<int, PyObject>(5);
                while ((op = (Opcode) reader.ReadByte()) != Opcode.Stop)
                {
                    switch (op)
                    {
                        case Opcode.BinInt:
                            _stack.Add(new PyInt(reader.ReadInt32()));
                            break;

                        case Opcode.BinInt1:
                            _stack.Add(new PyInt(reader.ReadByte()));
                            break;

                        case Opcode.BinInt2:
                            _stack.Add(new PyInt(reader.ReadInt16()));
                            break;

                        case Opcode.BinString:
                            {
                                var length = reader.ReadInt32();
                                _stack.Add(new PyString(reader.ReadBytes(length)));
                                break;
                            }

                        case Opcode.BinPut:
                            var index = reader.ReadByte();
                            memo.Add(index, _stack[_stack.Count - 1]);
                            break;

                        case Opcode.ShortBinString:
                            {
                                var length = reader.ReadByte();
                                _stack.Add(new PyString(reader.ReadBytes(length)));
                                break;
                            }

                        case Opcode.Tuple3:
                            {
                                var a = _stack.Pop();
                                var b = _stack.Pop();
                                var c = _stack.Pop();
                                _stack.Add(new PyTuple(a, b, c));
                                break;
                            }

                        case Opcode.Tuple2:
                            {
                                var a = _stack.Pop();
                                var b = _stack.Pop();
                                _stack.Add(new PyTuple(a, b));
                                break;
                            }

                        case Opcode.Tuple1:
                            _stack.Add(new PyTuple(_stack.Pop()));
                            break;

                        case Opcode.EmptyDict:
                            _stack.Add(new PyDict());
                            break;

                        case Opcode.Mark:
                            _stack.Add(_mark);
                            break;

                        case Opcode.EmptyList:
                            _stack.Add(new PyList());
                            break;

                        case Opcode.EmptyTuple:
                            _stack.Add(new PyTuple());
                            break;

                        case Opcode.BinGet:
                            _stack.Add(memo[reader.ReadByte()]);
                            break;

                        case Opcode.LongBinPut:
                            memo.Add(reader.ReadInt32(), _stack[_stack.Count - 1]);
                            break;

                        case Opcode.Long1:
                            {
                                var length = reader.ReadByte();
                                var data = reader.ReadBytes(length);
                                if (length > 8)
                                    throw new Exception("No support for integers exceeding 64 bits");
                                var filled = new byte[8];
                                for (int i = 0; i < data.Length; i++)
                                    filled[i] = data[i];
                                for (int i = data.Length; i < 8; i++)
                                    filled[i] = 0;
                                _stack.Add(new PyLong(BitConverter.ToInt64(filled, 0)));
                                break;
                            }

                        case Opcode.Appends:
                            {
                                int markIndex = FindMark();
                                var seq = _stack.ElementAt(markIndex - 1) as PyContainer;
                                if (seq == null)
                                    throw new Exception("Can only append to PyContainers (PyList, PyTuple)");
                                var sliceStart = markIndex + 1;
                                var sliceEnd = _stack.Count;
                                for (int i = sliceStart; i < sliceEnd; i++)
                                    seq.Append(_stack[i]);
                                // we need to remove the mark and we need to delete from top downwards, or indices are invalidated mid-loop
                                for (int i = sliceEnd - 1; i >= markIndex; i--)
                                    _stack.RemoveAt(i);
                                break;
                            }

                        case Opcode.NewFalse:
                            _stack.Add(new PyBool(false));
                            break;

                        case Opcode.NewTrue:
                            _stack.Add(new PyBool(true));
                            break;

                        case Opcode.SetItems:
                            {
                                int markIndex = FindMark();
                                var dict = _stack[markIndex - 1] as PyDict;
                                if (dict == null)
                                    throw new Exception("Expected PyDict for SetItems");
                                for (int i = markIndex + 1; i < _stack.Count; i += 2)
                                    dict.Dict.Add(_stack[i], _stack[i + 1]);
                                for (int i = _stack.Count - 1; i >= markIndex; i--)
                                    _stack.RemoveAt(i);
                                break;
                            }

                        default:
                            throw new NotSupportedException("Opcode " + op + " is not supported");
                    }
                }

                memo.Clear();
                if (_stack.Count > 0)
                    Result = _stack.Pop();
                _stack.Clear();
                GC.Collect();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Error in data at offset " + reader.BaseStream.Position + " (" + ex.Message + ")", ex);
            }
        }

        enum Opcode : byte
        {
            Append = (byte)'a',
            Appends = (byte)'e',
            BinFloat = (byte)'G',
            BinGet = (byte)'h',
            BinInt = (byte)'J',
            BinInt1 = (byte)'K',
            BinInt2 = (byte)'M',
            BinPersistentId = (byte)'Q',
            BinPut = (byte)'q',
            BinString = (byte)'T',
            BinUnicode = (byte)'X',
            Build = (byte)'b',
            Dictionary = (byte)'d',
            Dup = (byte)'2',
            EmptyDict = (byte)'}',
            EmptyList = (byte)']',
            EmptyTuple = (byte)')',
            Ext1 = 0x82,
            Ext2 = 0x83,
            Ext4 = 0x84,
            Float = (byte)'F',
            Get = (byte)'g',
            Global = (byte)'c',
            Inst = (byte)'i',
            Int = (byte)'I',
            List = (byte)'l',
            Long = (byte)'L',
            Long1 = 0x8a,
            Long4 = 0x8b,
            LongBinGet = (byte)'j',
            LongBinPut = (byte)'r',
            Mark = (byte)'(',
            NewFalse = 0x89,
            NewObject = 0x81,
            NewTrue = 0x88,
            NoneValue = (byte)'N',
            Obj = (byte)'o',
            PersistentId = (byte)'P',
            Pop = (byte)'0',
            PopMark = (byte)'1',
            Proto = 0x80,
            Put = (byte)'p',
            Reduce = (byte)'R',
            SetItem = (byte)'s',
            SetItems = (byte)'u',
            ShortBinString = (byte)'U',
            Stop = (byte)'.',
            String = (byte)'S',
            Tuple = (byte)'t',
            Tuple1 = 0x85,
            Tuple2 = 0x86,
            Tuple3 = 0x87,
            Unicode = (byte)'V'
        }
    }

    public static class ListExtensions
    {
        public static T Pop<T>(this List<T> list)
        {
            T ret = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return ret;
        }
    }

}