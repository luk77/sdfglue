//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.DataNodes.Signals;
using SingleDocAppCore.Input;
using SingleDocAppFramework.Ui.Windows;

namespace SdfGlueUi.Ui.Windows
{
    // "Inputs" window (registered only with DataModel.UseSignals): shows which signals of the project use a channel
    public class WndInputsSdfGlue : WndInputs
    {
        private IUiExecutorSdfGlue ExecutorSdfGlue { get { return (IUiExecutorSdfGlue)Executor; } }

        protected override string? GetChannelUsageInfo(InputChannel channel)
        {
            List<SignalInstance> users = ExecutorSdfGlue.GetModel().Signals.FindInputChannelUsers(channel.Id);
            if (users.Count == 0)
                return "no signals (in this project)";

            return String.Join(", ", users.Select(s => s.Name.Val));
        }
    }
}
