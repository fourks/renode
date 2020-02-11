//
// Copyright (c) 2010-2019 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using System.Collections.Generic;
using System.Linq;
using Antmicro.Renode.Core;
using Antmicro.Renode.Peripherals.UART;
using Antmicro.Renode.Testing;
using Antmicro.Renode.Time;

namespace Antmicro.Renode.RobotFramework
{
    internal class UartKeywords : TestersProvider<TerminalTester, IUART>, IRobotFrameworkKeywordProvider
    {
        public void Dispose()
        {
        }

        [RobotFrameworkKeyword]
        public string GetTerminalTesterReport(int? testerId = null)
        {
            return GetTesterOrThrowException(testerId).GetReport();
        }

        [RobotFrameworkKeyword]
        public int CreateTerminalTester(string uart, string prompt = null, int timeout = 30, string machine = null, string endLineOption = null)
        {
            return CreateNewTester(uartObject =>
            {
                TerminalTester tester;
                if(Enum.TryParse<EndLineOption>(endLineOption, out var result))
                {
                    tester = new TerminalTester(TimeInterval.FromSeconds((uint)timeout), result);
                }
                else
                {
                    tester = new TerminalTester(TimeInterval.FromSeconds((uint)timeout));
                }
                tester.AttachTo(uartObject);

                globalPrompt[tester] = prompt;

                return tester;
            }, uart, machine);
        }

        [RobotFrameworkKeyword]
        public void SetNewPromptForUart(string prompt, int? testerId = null)
        {
            globalPrompt[GetTesterOrThrowException(testerId)] = prompt;
        }

        [RobotFrameworkKeyword]
        public TerminalTesterResult WaitForPromptOnUart(string prompt = null, int? testerId = null, uint? timeout = null, bool treatAsRegex = false)
        {
            // this is a quite strange logic, but it's kept that way to be compatible with the previous implementation
            if(prompt != null && !globalPrompt.ContainsKey(GetTesterOrThrowException(testerId)))
            {
                SetNewPromptForUart(prompt, testerId);
            }

            return WaitForLineOnUart(prompt ?? globalPrompt[GetTesterOrThrowException(testerId)], timeout, testerId, treatAsRegex, true);
        }

        [RobotFrameworkKeyword]
        public TerminalTesterResult WaitForLineOnUart(string content, uint? timeout = null, int? testerId = null, bool treatAsRegex = false, bool charByCharEnabled = false)
        {
            TimeInterval? timeInterval = null;
            if(timeout.HasValue)
            {
                timeInterval = TimeInterval.FromSeconds(timeout.Value);
            }

            var tester = GetTesterOrThrowException(testerId);
            var result = tester.WaitFor(content, timeInterval, treatAsRegex, charByCharEnabled);
            if(result == null)
            {
                OperationFail(tester);
            }
            return result;
        }

        [RobotFrameworkKeyword]
        public TerminalTesterResult WaitForNextLineOnUart(uint? timeout = null, int? testerId = null)
        {
            TimeInterval? timeInterval = null;
            if(timeout.HasValue)
            {
                timeInterval = TimeInterval.FromSeconds(timeout.Value);
            }

            var tester = GetTesterOrThrowException(testerId);
            var result = tester.NextLine(timeInterval);
            if(result == null)
            {
                OperationFail(tester);
            }
            return result;
        }

        [RobotFrameworkKeyword]
        public TerminalTesterResult SendKeyToUart(byte c, int? testerId = null)
        {
            return WriteCharOnUart((char)c, testerId);
        }

        [RobotFrameworkKeyword]
        public TerminalTesterResult WriteCharOnUart(char c, int? testerId = null)
        {
            GetTesterOrThrowException(testerId).Write(c.ToString());
            return new TerminalTesterResult(string.Empty, 0);
        }

        [RobotFrameworkKeyword]
        public TerminalTesterResult WriteLineToUart(string content = "", int? testerId = null, bool waitForEcho = true)
        {
            var tester = GetTesterOrThrowException(testerId);
            tester.WriteLine(content);
            if(waitForEcho && tester.WaitFor(content, charByCharEnabled: true) == null)
            {
                OperationFail(tester);
            }
            return new TerminalTesterResult(string.Empty, 0);
        }

        [RobotFrameworkKeyword]
        public void TestIfUartIsIdle(uint timeInSeconds, int? testerId = null)
        {
            var tester = GetTesterOrThrowException(testerId);
            var result = tester.IsIdle(TimeInterval.FromSeconds(timeInSeconds));
            if(!result)
            {
                OperationFail(tester);
            }
        }

        private void OperationFail(TerminalTester tester)
        {
            throw new InvalidOperationException($"Terminal tester failed!\n\nFull report:\n{tester.GetReport()}");
        }

        private readonly Dictionary<TerminalTester, string> globalPrompt = new Dictionary<TerminalTester, string>();
    }
}
