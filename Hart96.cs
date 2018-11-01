using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeDrinkotechSever
{
    public class Hart96{
        private static Hart96 instance;

        private Hart96() {}

        public static Hart96 Instance
        {
            get{
                if(instance == null){

                    instance = new Hart96();
                }
                return instance;
            }

        }



    }
}
