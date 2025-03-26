using System;
using System.Collections.Generic;

namespace ESLFeeder.Models
{
    public class LeaveVariables
    {
        // Basic wage information
        public double ScheduledHours { get; set; }
        public double PayRate { get; set; }
        public bool IsPartialHours => ScheduledHours < 40;
        public double WeeklyWage { get; set; }
        public double BasicPay { get; set; }
        public double HoursPerWeek { get; set; }

        // CTPL calculations
        public double MinWage40 { get; set; }
        public double NinetyFiveCTMin40 { get; set; }
        public double CtplCalcStar { get; set; }
        public double CtplCalc { get; set; }
        public double CtplPayment { get; set; }

        // STD calculations
        public double StdOrNot { get; set; }

        // PTO calculations
        public double PtoSuppDollars { get; set; }
        public double PtoSuppHrs { get; set; }
        public double PtoReserve { get; set; }
        public double PtoAvailCalc { get; set; }
        public double PtoUsable { get; set; }
        public double PtoUseHrs { get; set; }

        // Basic Sick calculations
        public double BasicSickAvailCalc { get; set; }

        // Pay period information
        public int WeekOfPP { get; set; }

        // Additional fields needed for conditions
        public DateTime PayStartDate { get; set; }
        public DateTime PayEndDate { get; set; }
        public string? FmlaApprDate { get; set; }
        public string? CtplStart { get; set; }
        public string? CtplEnd { get; set; }
        public string? CtplForm { get; set; }
        public string? StdApprovedThrough { get; set; }
        public string? CtplApprovedAmount { get; set; }
        public string? EePtoRtw { get; set; }
        public double PtoHrsLast1Week { get; set; }
        public double PtoHrsLast2Week { get; set; }
        public double PtoAvail { get; set; }
        public double BasicSickLast1Week { get; set; }
        public double BasicSickLast2Week { get; set; }
        public double BasicSickAvail { get; set; }

        public bool HasCTPLForm { get; set; }
        public double CTPLApprovedAmount { get; set; }
        public string? EmployeeStatus { get; set; }
        public DateTime? CTPLStartDate { get; set; }
        public DateTime? CTPLEndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public double PTOHours { get; set; }
        public double LOANoPayHours { get; set; }
        
        // Scenario identification properties
        public string? ReasonCode { get; set; }
        public int GlCompany { get; set; }

        // Store the original row data for condition evaluation
        public object? RowData { get; set; }

        public Dictionary<int, Dictionary<string, bool>> EvaluatedConditions { get; set; } = new Dictionary<int, Dictionary<string, bool>>();
    }
} 