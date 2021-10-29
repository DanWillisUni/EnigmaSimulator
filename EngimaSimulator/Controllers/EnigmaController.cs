﻿using EngimaSimulator.Models;
using EngimaSimulator.Models.Enigma;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace EngimaSimulator.Controllers
{
    public class EnigmaController : Controller
    {
        private readonly ILogger<EnigmaController> _logger;
        private EnigmaModel enigmaModel { get; set; }

        public EnigmaController(ILogger<EnigmaController> logger)
        {
            _logger = logger;            
        }        
        public IActionResult Index()
        {
            MainViewModel model = new MainViewModel(enigmaModel);
            return View(model);
        }

        #region encoding
        private char encode(EnigmaModel em,char input)
        {
            char current = input;
            current = plugboardSwap(em.plugboard,current);
            foreach (RotorModel r in em.rotors.Reverse<RotorModel>())
            {
                current = rotorEncode(r,current);
            }
            current = rotorEncode(em.reflector, current);
            foreach (RotorModel r in em.rotors)
            {
                current = rotorEncode(r, current);
            }
            current = plugboardSwap(em.plugboard,current);
            return current;
        }
        private char plugboardSwap(Dictionary<char,char> plugboard,char input)
        {
            foreach (KeyValuePair<char, char> entry in plugboard)
            {
                if (entry.Key.Equals(input))
                {
                    return entry.Value;
                }
                if (entry.Value.Equals(input))
                {
                    return entry.Key;
                }
            }
            return input;
        }
        private char rotorEncode(RotorModel rm,char input)
        {
            return rm.rotor.order[(Convert.ToInt32(input) - 65 + rm.rotation) % 26];
        }
        #endregion


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}