﻿using EnigmaBreaker.Configuration.Models;
using EnigmaBreaker.Models;
using EnigmaBreaker.Services.Fitness;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SharedCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace EnigmaBreaker.Services
{
    public class Measuring
    {
        private readonly ILogger<Measuring> _logger;
        private readonly BasicConfiguration _bc;
        private readonly EncodingService _encodingService;
        private readonly IFitness.FitnessResolver _resolver;
        private readonly BasicService _bs;
        public Measuring(ILogger<Measuring> logger, BasicConfiguration bc, EncodingService encodingService, IFitness.FitnessResolver fitnessResolver,BasicService bs) 
        {
            _logger = logger;
            _bc = bc;
            _encodingService = encodingService;
            _resolver = fitnessResolver;
            _bs = bs;
        }

        public void root()
        {
            testLength(100, 2000, 100, "Plugboard", 100, new List<string>() { "IOC" }, "Results/plugboardLengthTest");
            //testLength(100, 2000, 100, "Plugboard", 100, new List<string>() { "IOC", "S", "BI", "TRI","QUAD" },"Results/plugboardLengthTest");//R 3.3 hours perfect
            ////testLength(5, 500, 5, "Plugboard", 500, new List<string>() { "IOC", "S", "BI", "TRI", "QUAD" }, "Results/plugboardLengthTestClose");//R Perfect
            //testLength(100, 2000, 100, "Offset", 50, new List<string>() { "IOC", "S", "BI", "TRI", "QUAD" },"Results/offsetLengthTest");//R 2.9 hours perfect
            //testLength(100, 2000, 100, "Rotors", 3, new List<string>() { "IOC", "S", "BI", "TRI", "QUAD" },"Results/rotorLengthTest");//R 29 hours

            //testIndex(100, 2000, 100, "Plugboard", 250, "Results/plugboardIndexSingleTest",1,2,1, "S");//R 13.8 hours perfect
            //testIndex(100, 2000, 100, "Plugboard", 100, "Results/plugboardIndexTest", 1, 3, 1, "F");//R 1 hour        
            //testIndex(100, 2000, 100, "Offset", 50, "Results/offsetIndexSingleTest", 1, 20, 1, "S");//R 12 hours perfect
            //testIndex(100, 2000, 100, "Offset", 50, "Results/offsetIndexTest", 1, 20, 1, "F");//12 hours
            ////testIndex(100, 2000, 100, "Rotors", 10, "Results/rotorsIndexSingleTest", 1, 3, 1, "S");
            //testIndex(100, 2000, 100, "Rotors", 10, "Results/rotorsIndexTest", 1, 3, 1, "F");

            //testSpeed(100, 2000, 100, "Plugboard", 5, "Results/plugboardSpeedTest", 1, 2, 1);//perfect
            //testSpeed(100, 2000, 100, "Offset", 5, "Results/offsetSpeedTest", 1, 26, 1);//R 1.5 perfect
            ////testSpeed(100, 2000, 100, "Rotors", 3, "Results/rotorsSpeedTest",1,3,1);//18 hours

            //measureFullRunthrough(100, 2000, 100,10, "Results/fullMeasureRefined");
            ////measureFullRunthrough(100, 2000, 100,10, "Results/fullMeasureUnrefined",true);//20 hours
        }
        public void measureFullRunthrough(int from, int to, int step, int iterations, string filePathAndName,bool withoutRefinement)
        {
            string plaintext = _bs.getText(to * 2);
            
            List<string> linesToFile = new List<string>() { ",RotorSuccess,OffsetSuccess,PlugboardSuccess,FullSuccess" };
            for (int i = from; i < to + 1; i += step)
            {                
                int[] plainArr = _encodingService.preProccessCiphertext(plaintext).ToList().GetRange(0, i).ToArray();

                int rotorSuccess = 0;
                int offsetSuccess = 0;
                int plugboardSuccess = 0;
                for (int currentIteration = 0; currentIteration < iterations; currentIteration++)
                {
                    EnigmaModel em = EnigmaModel.randomizeEnigma(_bc.numberOfRotorsInUse, _bc.numberOfReflectorsInUse, _bc.maxPlugboardSettings);
                    string emJson = JsonConvert.SerializeObject(em);
                    EnigmaModel em2 = JsonConvert.DeserializeObject<EnigmaModel>(emJson);

                    _logger.LogDebug(em.ToString());
                    int[] cipherArr = _encodingService.encode(plainArr, em);
                    BreakerConfiguration breakerConfiguration = new BreakerConfiguration(cipherArr.Length,withoutRefinement);
                    List<BreakerResult> initialRotorSetupResults = _bs.sortBreakerList(_bs.getRotorResults(cipherArr, breakerConfiguration));
                    bool found = false;
                    foreach (BreakerResult br in initialRotorSetupResults)
                    {
                        if (compareRotors(em2, br.enigmaModel))
                        {
                            found = true;
                            rotorSuccess++;
                            break;
                        }
                    }
                    if (found)
                    {
                        List<BreakerResult> fullRotorResultOfAll = _bs.getRotationOffsetResult(initialRotorSetupResults, cipherArr, breakerConfiguration);
                        found = false;
                        foreach (BreakerResult brr in fullRotorResultOfAll)
                        {
                            if (compareOffset(em2, brr.enigmaModel))
                            {
                                found = true;
                                offsetSuccess++;
                                break;
                            }
                        }
                        if (found)
                        {
                            found = false;
                            foreach (BreakerResult brrr in fullRotorResultOfAll)
                            {
                                List<BreakerResult> finalResult = _bs.getPlugboardResults(new List<BreakerResult>() { brrr }, cipherArr, breakerConfiguration);
                                if (comparePlugboard(em2.toStringPlugboard(), finalResult[0].enigmaModel.toStringPlugboard()))
                                {
                                    found = true;
                                    plugboardSuccess++;
                                    break;
                                }
                            }
                        }
                    }
                }
                double rotorSuccessRate = (double)(rotorSuccess * 100 / iterations);
                double offsetSuccessRate = (double)(offsetSuccess * 100 / (iterations - (iterations - rotorSuccess)));
                double plugboardSuccessRate = (double)(plugboardSuccess * 100 / (iterations - (iterations - rotorSuccess) - ((iterations - (iterations - rotorSuccess)) - offsetSuccess)));
                double fullSuccessRate = (double)(plugboardSuccess * 100 / iterations);
                _logger.LogDebug($"Rotor Success: {rotorSuccessRate}%");//RotorMiss = iteration - rotorSuccess
                _logger.LogDebug($"Offset Success: {offsetSuccessRate}%");//OffsetMiss = (iterations - (iterations-rotorSuccess)) - offsetSuccess
                _logger.LogDebug($"Plugboard Success: {plugboardSuccessRate}%");
                _logger.LogDebug($"Success: {fullSuccessRate}%");
                string lineToFile = $"{i},{rotorSuccessRate},{offsetSuccessRate},{plugboardSuccessRate},{fullSuccessRate}";
                linesToFile.Add(lineToFile);
            }

            File.WriteAllLines(filePathAndName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv", linesToFile);
            _logger.LogInformation("Writing to file");
        }
        public void testLength(int from, int to, int step, string toTest, int iterations, List<string> fitnessStrToTest, string filePathAndName)
        {
            string plaintext = _bs.getText(to * 2);
            List<int> plainArr = _encodingService.preProccessCiphertext(plaintext).ToList();
            List<string> linesToFile = new List<string>() { "," + string.Join(",", fitnessStrToTest) };
            for (int i = from; i < to + 1; i += step)
            {
                string lineToFile = $"{i}";
                foreach (string fitnessStr in fitnessStrToTest)
                {
                    switch (toTest)
                    {
                        case "Plugboard":
                            lineToFile += $", {testPlugboard(plainArr.GetRange(0, i).ToArray(), iterations, fitnessStr)}";
                            break;
                        case "Offset":
                            lineToFile += $", {testOffset(plainArr.GetRange(0, i).ToArray(), iterations, fitnessStr)}";
                            break;
                        case "Rotors":
                            lineToFile += $", {testRotor(plainArr.GetRange(0, i).ToArray(), iterations, fitnessStr)}";
                            break;
                        default:
                            _logger.LogWarning($"Unsure what to test: {toTest}");
                            break;
                    }
                    _logger.LogInformation($"Finished {fitnessStr}");
                }
                _logger.LogInformation($"Finished {toTest} {i}");
                linesToFile.Add(lineToFile);
            }
            
            File.WriteAllLines(filePathAndName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv", linesToFile);
            _logger.LogInformation("Writing to file");
        }

        public void testSpeed(int from, int to, int step, string toTest, int iterations, string filePathAndName, int singleFrom, int singleTo, int singleStep)
        {
            string plaintext = _bs.getText(to * 2);
            List<int> plainArr = _encodingService.preProccessCiphertext(plaintext).ToList();
            string s = "";
            for (int combinationValue = singleFrom; combinationValue <= singleTo; combinationValue += singleStep)
            {
                s += $", {combinationValue}";
            }
            List<string> linesToFile = new List<string>() { s };
            for (int i = from; i < to + 1; i += step)
            {
                string lineToFile = $"{i}";
                string elapsedTime = "";
                for (int combinationValue = singleFrom; combinationValue <= singleTo; combinationValue += singleStep)
                {
                    TimeSpan total = TimeSpan.Zero;
                    for (int j = 0; j < iterations; j++)
                    {
                        EnigmaModel em = EnigmaModel.randomizeEnigma(_bc.numberOfRotorsInUse, _bc.numberOfReflectorsInUse, _bc.maxPlugboardSettings);
                        string emJson = JsonConvert.SerializeObject(em);
                        int[] cipherArr = _encodingService.encode(plainArr.GetRange(0, i).ToArray(), em);
                        
                        BreakerConfiguration breakerConfiguration = new BreakerConfiguration(cipherArr.Length);

                        switch (toTest)
                        {
                            case "Plugboard":
                                EnigmaModel em2 = JsonConvert.DeserializeObject<EnigmaModel>(emJson);
                                em2.plugboard = new Dictionary<int, int>();
                                
                                breakerConfiguration.numberOfSinglePlugboardSettingsToKeep = combinationValue;
                                
                                Stopwatch stopWatchPlugboard = new Stopwatch();
                                stopWatchPlugboard.Start();
                                List<BreakerResult> finalResult = _bs.getPlugboardResults(new List<BreakerResult>() { new BreakerResult(cipherArr, double.MinValue, em2) }, cipherArr, breakerConfiguration);
                                stopWatchPlugboard.Stop();
                                TimeSpan tsPlugboard = stopWatchPlugboard.Elapsed;
                                total = total.Add(tsPlugboard);
                                break;
                            case "Offset":
                                em2 = JsonConvert.DeserializeObject<EnigmaModel>(emJson);
                                em2.plugboard = new Dictionary<int, int>();
                                Random rnd = new Random();
                                for (int ri = 0; ri < 3; ri++)
                                {
                                    em2.rotors[ri].rotation = EncodingService.mod26(em2.rotors[ri].rotation - em2.rotors[ri].ringOffset) + rnd.Next(3) - 1;
                                    em2.rotors[ri].ringOffset = 0;
                                }                                
                                breakerConfiguration.numberOfSettingsPerRotationCombinationToKeep = combinationValue;
                                
                                Stopwatch stopWatchOffset = new Stopwatch();
                                stopWatchOffset.Start();
                                List<BreakerResult> fullRotorOffset = _bs.getRotationOffsetResult(new List<BreakerResult>() { new BreakerResult(cipherArr, double.MinValue, em2) }, cipherArr, breakerConfiguration);
                                stopWatchOffset.Stop();
                                TimeSpan tsOffset = stopWatchOffset.Elapsed;
                                total = total.Add(tsOffset);
                                break;
                            case "Rotors":                                
                                breakerConfiguration.numberOfSettingsPerRotorCombinationToKeep = combinationValue;
                                
                                Stopwatch stopWatchRotors = new Stopwatch();
                                stopWatchRotors.Start();
                                List<BreakerResult> initialRotorSetupResults = _bs.sortBreakerList(_bs.getRotorResults(cipherArr, breakerConfiguration));
                                stopWatchRotors.Stop();
                                TimeSpan tsRotors = stopWatchRotors.Elapsed;
                                total = total.Add(tsRotors);
                                break;
                            default:
                                _logger.LogWarning($"Unsure what to test: {toTest}");
                                break;
                        }                        
                    }
                    TimeSpan ts = new TimeSpan(total.Ticks / iterations);
                    elapsedTime += $",{ts.Minutes}:{ts.Seconds}.{ts.Milliseconds}";
                }
                lineToFile += elapsedTime;
                linesToFile.Add(lineToFile);
                _logger.LogInformation($"Finished {toTest} {i}");
            }

            File.WriteAllLines(filePathAndName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv", linesToFile);
            _logger.LogInformation("Writing to file");
        }

        public void testIndex(int from, int to, int step, string toTest, int iterations, string filePathAndName,int singleFrom,int singleTo,int singleStep, string isTotal)
        {
            string plaintext = _bs.getText(to * 2);
            List<int> plainArr = _encodingService.preProccessCiphertext(plaintext).ToList();
            string s = "";
            for (int combinationValue = singleFrom; combinationValue <= singleTo; combinationValue += singleStep)
            {
                s += $", {combinationValue}";
            }
            List<string> linesToFile = new List<string>() { s };
            for (int messageLength = from; messageLength < to + 1; messageLength += step)
            {
                string lineToFile = $"{messageLength}";
                for (int combinationValue = singleFrom; combinationValue <= singleTo; combinationValue += singleStep)
                {
                    int missNumber = 0;
                    for (int currentIteration = 0; currentIteration < iterations; currentIteration++)
                    {
                        EnigmaModel em = EnigmaModel.randomizeEnigma(_bc.numberOfRotorsInUse, _bc.numberOfReflectorsInUse, _bc.maxPlugboardSettings);
                        string emJson = JsonConvert.SerializeObject(em);
                        int[] cipherArr = _encodingService.encode(plainArr.GetRange(0, messageLength).ToArray(), em);
                        BreakerConfiguration bc = new BreakerConfiguration(cipherArr.Length);
                        bc.numberOfSettingsPerRotorCombinationToKeep = 10;
                        bc.numberOfRotorsToKeep = 5;
                        bc.numberOfPlugboardSettingsToKeep = 1;
                        bc.numberOfSinglePlugboardSettingsToKeep = 1;
                        bc.numberOfOffsetToKeep = 5;
                        bc.numberOfSettingsPerRotationCombinationToKeep = 26;
                        EnigmaModel em2 = JsonConvert.DeserializeObject<EnigmaModel>(emJson);

                        switch (toTest)
                        {
                            case "Plugboard":
                                em2.plugboard = new Dictionary<int, int>();
                                if (isTotal == "F")
                                {
                                    bc.numberOfPlugboardSettingsToKeep = combinationValue;
                                }
                                else if (isTotal == "S")
                                {
                                    bc.numberOfSinglePlugboardSettingsToKeep = combinationValue;
                                }                                

                                List<BreakerResult> finalResults = _bs.getPlugboardResults(new List<BreakerResult>() { new BreakerResult(cipherArr, double.MinValue, em2) }, cipherArr, bc);
                                em2 = JsonConvert.DeserializeObject<EnigmaModel>(emJson);
                                bool foundP = false;
                                foreach (BreakerResult br in finalResults)
                                {
                                    if (comparePlugboard(em2.toStringPlugboard(), br.enigmaModel.toStringPlugboard()))
                                    {
                                        foundP = true;
                                        break;
                                    }
                                }
                                if (!foundP)
                                {
                                    missNumber++;
                                }
                                break;
                            case "Offset":
                                em2.plugboard = new Dictionary<int, int>();
                                Random rnd = new Random();
                                for (int ri = 0; ri < 3; ri++)
                                {
                                    em2.rotors[ri].rotation = EncodingService.mod26(em2.rotors[ri].rotation - em2.rotors[ri].ringOffset) + rnd.Next(3) - 1;
                                    em2.rotors[ri].ringOffset = 0;
                                }
                                
                                if (isTotal == "F")
                                {
                                    bc.numberOfOffsetToKeep = combinationValue;
                                }
                                else
                                {
                                    bc.numberOfSettingsPerRotationCombinationToKeep = combinationValue;
                                }

                                List<BreakerResult> fullRotorOffset = _bs.getRotationOffsetResult(new List<BreakerResult>() { new BreakerResult(cipherArr, double.MinValue, em2) }, cipherArr, bc);
                                em2 = JsonConvert.DeserializeObject<EnigmaModel>(emJson);
                                bool foundO = false;
                                foreach (BreakerResult br in fullRotorOffset)
                                {
                                    if (compareOffset(em2, br.enigmaModel))
                                    {
                                        foundO = true;
                                        break;
                                    }
                                }
                                if (!foundO)
                                {
                                    missNumber++;
                                }

                                
                                break;
                            case "Rotors":                                
                                if (isTotal == "F")
                                {
                                    bc.numberOfRotorsToKeep = combinationValue;
                                }
                                else
                                {
                                    bc.numberOfSettingsPerRotorCombinationToKeep = combinationValue;
                                }

                                List<BreakerResult> initialRotorSetupResults = _bs.sortBreakerList(_bs.getRotorResults(cipherArr, bc));
                                bool foundR = false;
                                foreach (BreakerResult br in initialRotorSetupResults)
                                {
                                    if (compareRotors(em2, br.enigmaModel))
                                    {
                                        foundR = true;
                                        break;
                                    }
                                }
                                if (!foundR)
                                {
                                    missNumber++;
                                }
                                
                                break;
                            default:
                                _logger.LogWarning($"Unsure what to test: {toTest}");
                                break;
                        }
                    }
                    lineToFile += "," + Convert.ToDouble(missNumber * 100 / iterations);                    
                }
                linesToFile.Add(lineToFile);
                _logger.LogInformation($"Finished {toTest} {messageLength}");
            }

            File.WriteAllLines(filePathAndName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv", linesToFile);
            _logger.LogInformation("Writing to file");
        }

        #region testing individual sections
        public double testRotor(int[] plaintext, int iterations, string fitnessStr = "")
        {
            double success = 0.0;
            for (int i = 0; i < iterations; i++)
            {
                EnigmaModel em = EnigmaModel.randomizeEnigma(_bc.numberOfRotorsInUse, _bc.numberOfReflectorsInUse, _bc.maxPlugboardSettings);
                string emJson = JsonConvert.SerializeObject(em);
                EnigmaModel em2 = JsonConvert.DeserializeObject<EnigmaModel>(emJson);

                _logger.LogDebug(em.ToString());
                int[] cipherArr = _encodingService.encode(plaintext, em);

                BreakerConfiguration breakerConfiguration = new BreakerConfiguration(cipherArr.Length);
                breakerConfiguration.RotorFitness = fitnessStr;
                List<BreakerResult> initialRotorSetupResults = _bs.sortBreakerList(_bs.getRotorResults(cipherArr,breakerConfiguration));

                foreach (BreakerResult br in initialRotorSetupResults)
                {
                    bool compareRotor = compareRotors(em2, br.enigmaModel);
                    if (compareRotor)
                    {
                        success += 1.0;
                        _logger.LogDebug($"Rotor result {initialRotorSetupResults.IndexOf(br)}: {br.enigmaModel.toStringRotors()}");
                        break;
                    }        
                }
            }
            _logger.LogDebug($"Success rate: {success * 100 / iterations}%");
            return success * 100 / iterations;
        }
        public double testOffset(int[] plaintext,int iterations,string fitnessStr = "")
        {
            double success = 0.0;
            for (int i = 0; i < iterations; i++)
            {
                EnigmaModel em = EnigmaModel.randomizeEnigma(_bc.numberOfRotorsInUse, _bc.numberOfReflectorsInUse, _bc.maxPlugboardSettings);
                string emJson = JsonConvert.SerializeObject(em);
                EnigmaModel em2 = JsonConvert.DeserializeObject<EnigmaModel>(emJson);
                EnigmaModel em3 = JsonConvert.DeserializeObject<EnigmaModel>(emJson);

                _logger.LogDebug(em.ToString());
                int[] cipherArr = _encodingService.encode(plaintext, em);

                em2.plugboard = new Dictionary<int, int>();
                Random rnd = new Random();
                for (int ri = 0; ri < 3; ri++)
                {
                    em2.rotors[ri].rotation = EncodingService.mod26(em2.rotors[ri].rotation - em2.rotors[ri].ringOffset) + rnd.Next(3) - 1;
                    em2.rotors[ri].ringOffset = 0;
                }
                _logger.LogDebug($"Input: {em2.toStringRotors()}");

                BreakerConfiguration breakerConfiguration = new BreakerConfiguration(cipherArr.Length);
                breakerConfiguration.OffsetFitness = fitnessStr;

                List<BreakerResult> fullRotorOffset = _bs.getRotationOffsetResult(new List<BreakerResult>() { new BreakerResult(cipherArr, double.MinValue, em2) }, cipherArr,breakerConfiguration);
                bool found = false;
                foreach (BreakerResult brr in fullRotorOffset)
                {
                    bool correctOffset = compareOffset(em3, brr.enigmaModel);
                    if (correctOffset)
                    {
                        found = true;
                        _logger.LogDebug($"Result {fullRotorOffset.IndexOf(brr)}: {brr.enigmaModel.toStringRotors()}");
                    }
                }
                if (found)
                {
                    success += 1.0;
                }
                else
                {
                    _logger.LogDebug("Incorrect");
                }
            }
            _logger.LogDebug($"Success rate: {success * 100 / iterations}%");
            return (success * 100) / iterations;
        }
        public double testPlugboard(int[] plaintext,int iterations,string fitnessStr = "")
        {
            double success = 0.0;
            for (int i = 0; i < iterations; i++)
            {
                EnigmaModel em = EnigmaModel.randomizeEnigma(_bc.numberOfRotorsInUse, _bc.numberOfReflectorsInUse, _bc.maxPlugboardSettings);
                string emJson = JsonConvert.SerializeObject(em);
                EnigmaModel em2 = JsonConvert.DeserializeObject<EnigmaModel>(emJson);

                _logger.LogDebug(em.ToString());
                int[] cipherArr = _encodingService.encode(plaintext, em);

                BreakerConfiguration breakerConfiguration = new BreakerConfiguration(cipherArr.Length);
                breakerConfiguration.PlugboardFitness = fitnessStr;

                em2.plugboard = new Dictionary<int, int>();                
                List<BreakerResult> finalResult = _bs.getPlugboardResults(new List<BreakerResult>() { new BreakerResult(cipherArr, double.MinValue, em2) }, cipherArr,breakerConfiguration);
                foreach (BreakerResult br in finalResult)
                {
                    bool correctPB = comparePlugboard(em.toStringPlugboard(), br.enigmaModel.toStringPlugboard());
                    if (correctPB)
                    {
                        success += 1.0;
                    }
                }
            }
            _logger.LogDebug($"Success rate: {success * 100 / iterations}%");
            return (success * 100) / iterations;
        }
        #endregion

        #region helper
        public bool compareRotors(EnigmaModel actual,EnigmaModel attempt)
        {
            if (attempt.reflector.rotor.name == actual.reflector.rotor.name && attempt.rotors[0].rotor.name == actual.rotors[0].rotor.name && attempt.rotors[1].rotor.name == actual.rotors[1].rotor.name && attempt.rotors[2].rotor.name == actual.rotors[2].rotor.name)
            {
                if (attempt.rotors[0].rotation - 1 == EncodingService.mod26(actual.rotors[0].rotation - actual.rotors[0].ringOffset) || attempt.rotors[0].rotation == EncodingService.mod26(actual.rotors[0].rotation - actual.rotors[0].ringOffset) || attempt.rotors[0].rotation + 1 == EncodingService.mod26(actual.rotors[0].rotation - actual.rotors[0].ringOffset))
                {
                    if (attempt.rotors[1].rotation - 1 == EncodingService.mod26(actual.rotors[1].rotation - actual.rotors[1].ringOffset) || attempt.rotors[1].rotation == EncodingService.mod26(actual.rotors[1].rotation - actual.rotors[1].ringOffset) || attempt.rotors[1].rotation + 1 == EncodingService.mod26(actual.rotors[1].rotation - actual.rotors[1].ringOffset))
                    {
                        if (attempt.rotors[2].rotation - 1 == EncodingService.mod26(actual.rotors[2].rotation - actual.rotors[2].ringOffset) || attempt.rotors[2].rotation == EncodingService.mod26(actual.rotors[2].rotation - actual.rotors[2].ringOffset) || attempt.rotors[2].rotation + 1 == EncodingService.mod26(actual.rotors[2].rotation - actual.rotors[2].ringOffset))//this line is a cheat
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public bool compareOffset(EnigmaModel actual,EnigmaModel attempt)
        {
            if (attempt.rotors[0].rotation == EncodingService.mod26(actual.rotors[0].rotation - actual.rotors[0].ringOffset) && attempt.rotors[0].ringOffset == 0)
            {
                string[] actualSplit = actual.toStringRotors().Split("/");
                string actualStr = actualSplit[0]  + "/" + actualSplit[2] + "/" + actualSplit[3];
                string[] attemptSplit = attempt.toStringRotors().Split("/");
                string attemptStr = attemptSplit[0] + "/" + attemptSplit[2] + "/" + attemptSplit[3];
                if(actualStr == attemptStr)
                {
                    return true;
                }
            }
            return false;
        }
        public bool comparePlugboard(string actual, string attempt)
        {
            if (actual.Length == attempt.Length)
            {
                foreach (string a in actual.Split(" "))
                {
                    bool found = false;
                    foreach (string r in attempt.Split(" "))
                    {
                        if (r == a)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}