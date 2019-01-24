namespace Pk3Maker
{
    public class Shader
    {
        public string shaderName;
        public string shaderFile;
        public bool hasCurlyBraceInName;

        public Shader(string name, string file, bool hasCurlyBrace)
        {
            shaderName = name;
            shaderFile = file;
            hasCurlyBraceInName = hasCurlyBrace;
        }
    }

}