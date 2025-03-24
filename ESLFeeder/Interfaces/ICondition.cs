using System;
using System.Data;
using System.Collections.Generic;
using ESLFeeder.Models;

namespace ESLFeeder.Interfaces
{
    public interface ICondition
    {
        string Name { get; }
        string Description { get; }
        
        bool Evaluate(DataRow row, LeaveVariables variables);
    }
} 