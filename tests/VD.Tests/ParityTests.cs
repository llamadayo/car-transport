using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using VD.Core;
using VD.Services;
namespace VD.Tests {
 public class ParityTests {
  private static JObject Fixture => JObject.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"Fixtures","parity.json")));
  public static IEnumerable<TestCaseData> MatchCases => Fixture["Matches"].Select(x=>new TestCaseData(x.ToString(Formatting.None)).SetName("JS parity: "+(string)x["Name"]));
  public static IEnumerable<TestCaseData> LoadCases => Fixture["Loads"].Select(x=>new TestCaseData(x.ToString(Formatting.None)).SetName("JS load: "+(string)x["Name"]));
  [TestCaseSource(nameof(MatchCases))] public void MatchOriginal(string json) {
   var c=JObject.Parse(json);var master=Fixture["Master"].ToObject<MasterSnapshot>();
   var actual=new RegionalMatchingService().Match(c["Application"].ToObject<RegionalApplication>(),master,c["Existing"].ToObject<List<RegionalApplication>>(),new List<Trip>(),DateTime.Parse((string)c["Now"]));
   var expected=c["Expected"].ToObject<MatchResult>();
   Assert.Multiple(()=>{Assert.That(actual.Ok,Is.EqualTo(expected.Ok));Assert.That(actual.Reason,Is.EqualTo(expected.Reason));Assert.That(actual.ShiftId,Is.EqualTo(expected.ShiftId));Assert.That(actual.ArrivalMinute,Is.EqualTo(expected.ArrivalMinute));Assert.That(actual.ExpectDiffMin,Is.EqualTo(expected.ExpectDiffMin));});
  }
  [TestCaseSource(nameof(LoadCases))] public void LoadOriginal(string json) {
   var c=JObject.Parse(json);var m=Fixture["Master"].ToObject<MasterSnapshot>();var result=new LoadFeasibilityService(m).Check(c["Items"].ToObject<List<CargoItem>>(),m.Vehicles.Single(v=>v.Id==(string)c["VehicleId"]));var expected=c["Expected"];
   Assert.Multiple(()=>{Assert.That(result.Ok,Is.EqualTo((bool)expected["Ok"]));Assert.That(result.Used.Volume,Is.EqualTo((double)expected["Volume"]).Within(1e-7));Assert.That(result.Used.Weight,Is.EqualTo((double)expected["Weight"]).Within(1e-7));Assert.That(result.Used.Floor,Is.EqualTo((double)expected["Floor"]).Within(1e-7));});
  }
 }
}
