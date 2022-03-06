﻿using EnigmaBreaker.Configuration.Models;
using EnigmaBreaker.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnigmaBreaker.Services.Fitness
{
    public class weightFitness : IFitness
    {
        private readonly FitnessConfiguration _fc;
        private readonly IFitness.FitnessResolver _resolver;
        private readonly CSVReaderService<WeightFile> _csvR;
        public weightFitness(FitnessConfiguration fc,IFitness.FitnessResolver resolver,CSVReaderService<WeightFile> csvR)
        {
            _resolver = resolver;
            _fc = fc;
            _csvR = csvR;
        }
        public double getFitness(int[] input, IFitness.Part part = IFitness.Part.None)
        {
            double r = getHitRate(input.Length, "IOC", part) * _resolver("IOC").getFitness(input);
            foreach (string fitnessStr in new List<string>() { })
                r += _fc.fitnessWeights[fitnessStr] * getHitRate(input.Length,fitnessStr,part) * _resolver(fitnessStr).getFitness(input) / input.Length;
            return r;
        }

        private double getHitRate(int length, string fitnessString, IFitness.Part part = IFitness.Part.None)
        {
            string fileName = "";
            if (part == IFitness.Part.Rotor)
            {
                fileName = _fc.weightFiles.rotorFileName;
            }
            else if (part == IFitness.Part.Offset)
            {
                fileName= _fc.weightFiles.offsetFileName;
            }
            else if(part == IFitness.Part.Plugboard)
            {
                fileName = _fc.weightFiles.plugboardFileName;
            }
            List<WeightFile> wf = _csvR.readFromFile(_fc.weightFiles.dir,fileName);
            int previous = 0;
            double r = -1;
            foreach (WeightFile w in wf)
            {
                if (length <= Convert.ToInt32(w.length) && length > previous)
                {
                    r = w.weights[fitnessString];
                    break;
                }
                previous = Convert.ToInt32(w.length);
            }
            if (r == -1)
            {
                r = wf[-1].weights[fitnessString];
            }
            return r;
        }
    }
}
