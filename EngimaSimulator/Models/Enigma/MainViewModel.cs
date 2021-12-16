﻿using Microsoft.AspNetCore.Mvc;
using SharedCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EngimaSimulator.Models.Enigma
{
    public class MainViewModel
    {
        public EnigmaModel enigmaModel { get; set; }
        public string Command { get; set; }
        public string outputTextBox { get; set; }
        public string inputTextBox { get; set; }
        public MainViewModel()
        {
            this.enigmaModel = new EnigmaModel();
            outputTextBox = "";
        }
        public MainViewModel(EnigmaModel enigmaModel)
        {
            this.enigmaModel = enigmaModel;
            outputTextBox = "";
        }
        
    }
}
