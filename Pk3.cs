using System.Collections.Generic;

namespace Pk3Maker
{
    public class Pk3
    {
        public List<string> cfg_maps;
        public List<string> env;
        public List<string> levelshots;
        public List<string> maps;
        public List<string> scripts;
        public List<string> sound;
        public List<string> textures;

        public Pk3(
            List<string> cfg_maps,
            List<string> env,
            List<string> levelshots,
            List<string> maps,
            List<string> scripts,
            List<string> sound,
            List<string> textures
            )
        {
            this.cfg_maps = cfg_maps;
            this.env = env;
            this.levelshots = levelshots;
            this.maps = maps;
            this.scripts = scripts;
            this.sound = sound;
            this.textures = textures;
        }
    }
}