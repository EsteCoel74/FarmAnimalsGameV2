using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarmAnimalsGameV2
{
    public class  Animal
    {
        public string Nom { get; set; }
        public string ImagePath { get; set; }
    }
    internal class AnimalRepository
    {
        // ══════════════════════════════════════════════════════════════
        //  DICTIONNAIRE RFID → ANIMAL
        // ══════════════════════════════════════════════════════════════
        public static readonly Dictionary<string, Animal> ParCarteRFID = new()
        {
            { "6FDB2B3E",       new Animal { Nom = "Vache",  ImagePath = "Assets/vache.png"  } },
            { "32FE281D",       new Animal { Nom = "Mouton", ImagePath = "Assets/mouton.png" } },
            { "42EA691D",       new Animal { Nom = "Chèvre", ImagePath = "Assets/chevre.png" } },
            { "6EAD4A74",       new Animal { Nom = "Cochon", ImagePath = "Assets/cochon.png" } },
            { "EE455374",       new Animal { Nom = "Cheval", ImagePath = "Assets/cheval.png" } },
            { "3E5D4A74",       new Animal { Nom = "Âne",    ImagePath = "Assets/ane.png"    } },
            { "0432486AD11990", new Animal { Nom = "Lapin",  ImagePath = "Assets/lapin.png"  } },
            { "04444A7AFB1990", new Animal { Nom = "Coq",    ImagePath = "Assets/coq.png"    } },

            // ── 2 cartes restantes à scanner ──────────────────────────
            { "041213A8E1190",  new Animal { Nom = "Oie",   ImagePath = "Assets/oie.png"   } },
            { "042D33D2FC1090", new Animal { Nom = "Poule", ImagePath = "Assets/poule.png" } },
        };
    }
}
