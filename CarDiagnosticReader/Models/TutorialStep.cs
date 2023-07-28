using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CarDiagnosticReader.Models
{
    public class TutorialStep
    {
        public string Instruction { get; set; }
        public Button TargetButton { get; set; }

        public TutorialStep(string instruction, Button targetButton)
        {
            Instruction = instruction;
            TargetButton = targetButton;
        }
    }
}
