﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharedCL
{
    public class RotorModel
    {
        public Rotor rotor { get; set; }
        public int rotation { get; set; }
        public int ringOffset { get; set; }
        public RotorModel(Rotor rotor, int rotation = 0, int ringOffset = 0)
        {
            this.rotor = rotor;
            this.rotation = rotation;
            this.ringOffset = ringOffset;
        }
        /// <summary>
        /// Override the normal to string
        /// </summary>
        /// <returns>Name, rotation, ring offset</returns>
        public override string ToString()
        {
            return $"{rotor.name}, {rotation}, {ringOffset}";
        }
    }
    public class Rotor
    {
        public string name { get; set; }
        public string order { get; set; }
        public int turnoverNotchA { get; set; }
        /// <summary>
        /// Overrides to string
        /// </summary>
        /// <returns>name, order, turnover notch</returns>
        public override string ToString()
        {
            return $"{name}, {order}, {turnoverNotchA}";
        }
    }
}
