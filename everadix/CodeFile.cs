namespace everadix
{

    public enum CodeType
    {
        Library,
        Game
    }

    public class CodeFile
    {
        public CodeType Type { get; private set; }
        public string Path { get; private set; }
        public byte[] Data { get; private set; }
        public bool MissingHeader { get; private set; }

        public CodeFile(CodeType type, string path, byte[] data, bool missingHeader = false)
        {
            Type = type;
            Path = path;
            Data = data;
            MissingHeader = missingHeader;
        }
    }

}