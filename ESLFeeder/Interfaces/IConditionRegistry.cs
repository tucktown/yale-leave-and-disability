using System.Data;
using ESLFeeder.Models;

namespace ESLFeeder.Interfaces
{
    public interface IConditionRegistry
    {
        bool EvaluateConditions(string[] conditionIds, DataRow row, LeaveVariables variables);
        void RegisterCondition(string id, ICondition condition);
        ICondition GetCondition(string id);
    }
} 