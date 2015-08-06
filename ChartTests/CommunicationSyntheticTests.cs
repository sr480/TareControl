using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChartTests
{
    [TestClass]
    public class CommunicationSyntheticTests
    {
        private const string ANSWERPARCE_EXPR = @"^<(?<NUM>\d+):(?<BODY>.*)>$";
        private const string BODYPARCE_EXPR = @"^((?<ARG>\S+)(\s|$))+$";

        [TestMethod]
        public void TestOKAnswerParcer()
        {
            string testAnswer = "<123:OK>";
            var answerParceRegX = new System.Text.RegularExpressions.Regex(ANSWERPARCE_EXPR, System.Text.RegularExpressions.RegexOptions.Compiled);
            var match = answerParceRegX.Match(testAnswer);

            if(!match.Success)
                throw new Exception("Error parcing answer");

            string body = match.Groups["BODY"].Value;
            if (body == "OK")
                return;
            else
                Assert.Fail();
        }
        [TestMethod]
        public void TestBodyAnswerParcer()
        {
            string testAnswer = "<123:-22 NOSENSOR>";
            var answerParceRegX = new System.Text.RegularExpressions.Regex(ANSWERPARCE_EXPR, System.Text.RegularExpressions.RegexOptions.Compiled);
            var match = answerParceRegX.Match(testAnswer);

            if (!match.Success)
                throw new Exception("Error parcing answer");

            string body = match.Groups["BODY"].Value;
            if (body == "OK")
                Assert.Fail();
            else
            {
                var bodyParcerRegX = new System.Text.RegularExpressions.Regex(BODYPARCE_EXPR, System.Text.RegularExpressions.RegexOptions.Compiled);
                var matchArg = bodyParcerRegX.Match(body);
                if (!matchArg.Success)
                    Assert.Fail();
                foreach (System.Text.RegularExpressions.Capture argument in matchArg.Groups["ARG"].Captures)
                {
                    if (argument.Value != "-22" & argument.Value != "NOSENSOR")
                        Assert.Fail();
                }
            }
        }
    }
}
