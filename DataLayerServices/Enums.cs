using System;
using System.Collections.Generic;
using System.Text;

namespace DataLayerServices
{
    public static class Enums
    {
        public enum Category
        {
            Rookie, Amateur, Pro, Master, Elite
        }
        public enum PointSystem
        {
            F1, Linear, Gpro, OldLinear
        }
        public enum TipoClassifica
        {
            it, fr, ar, all
        }
        public enum MainMenuOption
        {
            ViewStandings, Exit
        }
    }
}
