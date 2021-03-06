﻿//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="SpecRunSingleResults.cs" company="PicklesDoc">
//  Copyright 2011 Jeffrey Cameron
//  Copyright 2012-present PicklesDoc team and community contributors
//
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using PicklesDoc.Pickles.ObjectModel;

using Gherkin = PicklesDoc.Pickles.Parser;
using SpecRun = PicklesDoc.Pickles.Parser.SpecRun;

namespace PicklesDoc.Pickles.TestFrameworks
{
    public class SpecRunSingleResults : ITestResults
    {
        private readonly List<SpecRun.Feature> specRunFeatures;

        public SpecRunSingleResults(FileInfoBase fileInfo)
        {
            var resultsDocument = this.ReadResultsFile(fileInfo);

            this.specRunFeatures =
                resultsDocument.Descendants("feature").Select(SpecRun.Factory.ToSpecRunFeature).ToList();
        }

        public TestResult GetFeatureResult(Feature feature)
        {
            if (this.specRunFeatures == null)
            {
                return TestResult.Inconclusive;
            }

            var specRunFeature = this.FindSpecRunFeature(feature);

            if (specRunFeature == null)
            {
                return TestResult.Inconclusive;
            }

            TestResult result =
                specRunFeature.Scenarios.Select(specRunScenario => StringToTestResult(specRunScenario.Result)).Merge();

            return result;
        }

        public TestResult GetScenarioOutlineResult(ScenarioOutline scenarioOutline)
        {
            if (this.specRunFeatures == null)
            {
                return TestResult.Inconclusive;
            }

            var specRunFeature = this.FindSpecRunFeature(scenarioOutline.Feature);

            if (specRunFeature == null)
            {
                return TestResult.Inconclusive;
            }

            SpecRun.Scenario[] specRunScenarios = FindSpecRunScenarios(scenarioOutline, specRunFeature);

            if (specRunScenarios.Length == 0)
            {
                return TestResult.Inconclusive;
            }

            TestResult result = StringsToTestResult(specRunScenarios.Select(srs => srs.Result));

            return result;
        }

        public TestResult GetScenarioResult(Scenario scenario)
        {
            if (this.specRunFeatures == null)
            {
                return TestResult.Inconclusive;
            }

            var specRunFeature = this.FindSpecRunFeature(scenario.Feature);

            if (specRunFeature == null)
            {
                return TestResult.Inconclusive;
            }

            var specRunScenario = FindSpecRunScenario(scenario, specRunFeature);

            if (specRunScenario == null)
            {
                return TestResult.Inconclusive;
            }

            return StringToTestResult(specRunScenario.Result);
        }

        public TestResult GetExampleResult(ScenarioOutline scenario, string[] exampleValues)
        {
            throw new NotSupportedException();
        }

        public bool SupportsExampleResults
        {
            get { return false; }
        }

        private static TestResult StringsToTestResult(IEnumerable<string> results)
        {
            if (results == null)
            {
                return TestResult.Inconclusive;
            }

            return results.Select(StringToTestResult).Merge();
        }

        private static TestResult StringToTestResult(string result)
        {
            if (result == null)
            {
                return TestResult.Inconclusive;
            }

            switch (result.ToLowerInvariant())
            {
                case "passed":
                {
                    return TestResult.Passed;
                }

                case "failed":
                {
                    return TestResult.Failed;
                }

                default:
                {
                    return TestResult.Inconclusive;
                }
            }
        }

        private static SpecRun.Scenario[] FindSpecRunScenarios(ScenarioOutline scenarioOutline, SpecRun.Feature specRunFeature)
        {
            return specRunFeature.Scenarios.Where(d => d.Title.StartsWith(scenarioOutline.Name + ", ")).ToArray();
        }

        private static SpecRun.Scenario FindSpecRunScenario(Scenario scenario, SpecRun.Feature specRunFeature)
        {
            SpecRun.Scenario result = specRunFeature.Scenarios.FirstOrDefault(d => d.Title.Equals(scenario.Name));

            return result;
        }

        private SpecRun.Feature FindSpecRunFeature(Feature feature)
        {
            return this.specRunFeatures.FirstOrDefault(specRunFeature => specRunFeature.Title == feature.Name);
        }

        private XDocument ReadResultsFile(FileInfoBase testResultsFile)
        {
            XDocument document;
            using (var stream = testResultsFile.OpenRead())
            {
                using (var streamReader = new System.IO.StreamReader(stream))
                {
                    string content = streamReader.ReadToEnd();

                    int begin = content.IndexOf("<!-- Pickles Begin", StringComparison.Ordinal);

                    content = content.Substring(begin);

                    content = content.Replace("<!-- Pickles Begin", string.Empty);

                    int end = content.IndexOf("Pickles End -->", System.StringComparison.Ordinal);

                    content = content.Substring(0, end);

                    content = content.Replace("&lt;", "<").Replace("&gt;", ">");

                    var xmlReader = XmlReader.Create(new System.IO.StringReader(content));
                    document = XDocument.Load(xmlReader);
                }
            }

            return document;
        }
    }
}
