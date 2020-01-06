using System;
using ReductionFichierTexte;

namespace Motus 
{
    class Program
    {
        public static void Main ()
        {
            //Reductor.LancerReduction();

            CLI gameInterface = new CLI();
            gameInterface.Start();
        }
    }
}