using System;
using System.Collections.Generic;
using System.Data;
using ESLFeeder.Interfaces;
using ESLFeeder.Models;

namespace ESLFeeder.Models.Conditions
{
    public class C14 : ICondition
    {
        public string Name => "C14";
        public string Description => "PTO hours that are usable in combination with CT PL. If greater than 0, PTO will be used. If not, no PTO will be used.";

        public bool Evaluate(DataRow row, LeaveVariables variables)
        {
            return variables.PtoUseHrs > 0;
        }
        
        public bool Evaluate(Dictionary<string, object> data, LeaveVariables variables)
        {
            return Evaluate((DataRow)null, variables);
        }
    }
}

