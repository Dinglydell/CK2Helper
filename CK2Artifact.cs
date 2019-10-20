using PdxFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK2Helper
{
    public struct CK2Artifact
    {
        public CK2CharacterBase Owner { get; set; }

        public bool Equipped { get; set; }

        public string Type { get; set; }

        public DateTime Obtained { get; set; }

        public DateTime Created { get; set; }

        public CK2Artifact(CK2World world, PdxSublist data)
        {
            Owner = world.CK2Characters[(int)data.GetFloat("owner")];
            
            Equipped = data.GetBool("equipped");

            Type = data.GetString("type");

            Obtained = data.GetDate("obtained");

            Created = data.GetDate("obtained");

            Owner.AddArtifact(this);
        }
    }
}
