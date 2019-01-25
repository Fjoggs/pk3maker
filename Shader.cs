namespace Pk3Maker
{
    public class Shader
    {
        public string shaderName;
        public string shaderFile;
        public bool hasCurlyBraceInName;

        public Shader(string shaderName, string shaderFile, bool hasCurlyBraceInName)
        {
            this.shaderName = shaderName;
            this.shaderFile = shaderFile;
            this.hasCurlyBraceInName = hasCurlyBraceInName;
        }

        public bool isDuplicateShader(string shaderName)
        {
            return this.shaderName == shaderName;
        }
    }

}