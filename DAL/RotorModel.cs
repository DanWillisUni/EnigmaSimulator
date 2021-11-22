﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DAL
{
    public class RotorModel
    {
        public Rotor rotor { get; set; }
        public int rotation { get; set; }
        public int ringOffset { get; set; }
        public RotorModel(Rotor rotor,int rotation = 0,int ringOffset = 0)
        {
            this.rotor = rotor;
            this.rotation = rotation;
        }        
    }
    public class Rotor
    {
        public string name { get; set; }
        public string order { get; set; }
        public List<char> turnoverNotches { get; set; }
    }
}