using System;
using System.Data;
using ESLFeeder.Models;

namespace ESLFeeder.Services
{
    public class VariableCalculator : IVariableCalculator
    {
        private const double MIN_WAGE = 16.35;
        private const double MAX_CTPL_PAY = 981;

        public bool ValidateInputVariables(DataRow row, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(row["SCHED_HRS"]?.ToString()))
            {
                errorMessage = "Invalid SCHED_HRS value";
                return false;
            }

            if (string.IsNullOrEmpty(row["PAY_RATE"]?.ToString()))
            {
                errorMessage = "Invalid PAY_RATE value";
                return false;
            }

            return true;
        }

        public bool CalculateVariables(DataRow row, out LeaveVariables variables)
        {
            variables = new LeaveVariables();
            
            try
            {
                Console.WriteLine("Starting variable calculations...");

                // Set basic information
                Console.WriteLine($"SCHED_HRS: {row["SCHED_HRS"]}");
                variables.ScheduledHours = Convert.ToDouble(row["SCHED_HRS"]);
                Console.WriteLine($"PAY_RATE: {row["PAY_RATE"]}");
                variables.PayRate = Convert.ToDouble(row["PAY_RATE"]);
                Console.WriteLine($"WEEK_OF_PP: {row["WEEK_OF_PP"]}");
                variables.WeekOfPP = Convert.ToInt32(row["WEEK_OF_PP"]);
                Console.WriteLine($"PAY_START_DATE: {row["PAY_START_DATE"]}");
                variables.PayStartDate = Convert.ToDateTime(row["PAY_START_DATE"]);
                Console.WriteLine($"PAY_END_DATE: {row["PAY_END_DATE"]}");
                variables.PayEndDate = Convert.ToDateTime(row["PAY_END_DATE"]);

                // Set condition-related fields
                Console.WriteLine($"FMLA_APPR_DATE: {row["FMLA_APPR_DATE"]}");
                variables.FmlaApprDate = row["FMLA_APPR_DATE"]?.ToString();
                Console.WriteLine($"CTPL_START_DATE: {row["CTPL_START_DATE"]}");
                variables.CtplStart = row["CTPL_START_DATE"]?.ToString();
                if (DateTime.TryParse(variables.CtplStart, out var ctplStartDate))
                {
                    variables.CTPLStartDate = ctplStartDate;
                }
                Console.WriteLine($"CTPL_END_DATE: {row["CTPL_END_DATE"]}");
                variables.CtplEnd = row["CTPL_END_DATE"]?.ToString();
                if (DateTime.TryParse(variables.CtplEnd, out var ctplEndDate))
                {
                    variables.CTPLEndDate = ctplEndDate;
                }
                Console.WriteLine($"CTPL_FORM: {row["CTPL_FORM"]}");
                variables.CtplForm = row["CTPL_FORM"]?.ToString();
                variables.HasCTPLForm = variables.CtplForm?.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) ?? false;
                Console.WriteLine($"STD_APPROVED_THROUGH: {row["STD_APPROVED_THROUGH"]}");
                variables.StdApprovedThrough = row["STD_APPROVED_THROUGH"]?.ToString();
                if (DateTime.TryParse(variables.StdApprovedThrough, out var stdApprovedThrough))
                {
                    variables.ActualEndDate = stdApprovedThrough;
                }
                Console.WriteLine($"CTPL_APPROVED_AMOUNT: {row["CTPL_APPROVED_AMOUNT"]}");
                variables.CtplApprovedAmount = row["CTPL_APPROVED_AMOUNT"]?.ToString();
                Console.WriteLine($"EE_PTO_RTW: {row["EE_PTO_RTW"]}");
                variables.EePtoRtw = row["EE_PTO_RTW"]?.ToString();
                Console.WriteLine($"PTO_HRS_LASTWEEK: {row["PTO_HRS_LASTWEEK"]}");
                variables.PtoHrsLast1Week = row["PTO_HRS_LASTWEEK"]?.ToString();
                Console.WriteLine($"PTO_HRS_LAST_TWOWEEK: {row["PTO_HRS_LAST_TWOWEEK"]}");
                variables.PtoHrsLast2Week = row["PTO_HRS_LAST_TWOWEEK"]?.ToString();
                Console.WriteLine($"PTO_AVAIL: {row["PTO_AVAIL"]}");
                variables.PtoAvail = row["PTO_AVAIL"]?.ToString();
                Console.WriteLine($"BASIC_SICK_HRS: {row["BASIC_SICK_HRS"]}");
                variables.BasicSickLast1Week = row["BASIC_SICK_HRS"]?.ToString();
                Console.WriteLine($"BRIDGEPORT_SICK_HRS: {row["BRIDGEPORT_SICK_HRS"]}");
                variables.BasicSickLast2Week = row["BRIDGEPORT_SICK_HRS"]?.ToString();
                Console.WriteLine($"BH_SICK_AVAIL: {row["BH_SICK_AVAIL"]}");
                variables.BasicSickAvail = row["BH_SICK_AVAIL"]?.ToString();
                Console.WriteLine($"EMP_STATUS: {row["EMP_STATUS"]}");
                variables.EmployeeStatus = row["EMP_STATUS"]?.ToString();

                Console.WriteLine("Starting calculations...");

                // Basic wage calculations
                variables.WeeklyWage = variables.PayRate * variables.ScheduledHours;
                Console.WriteLine($"WeeklyWage: {variables.WeeklyWage}");
                
                // CTPL calculations
                variables.MinWage40 = MIN_WAGE * 40;
                variables.NinetyFiveCTMin40 = variables.MinWage40 * 0.95;
                variables.CtplCalcStar = (variables.WeeklyWage - variables.MinWage40) * 0.6;
                variables.CtplCalc = variables.NinetyFiveCTMin40 + variables.CtplCalcStar;
                Console.WriteLine($"CTPL Calc: {variables.CtplCalc}");

                // CTPL Payment
                variables.CtplPayment = string.IsNullOrEmpty(variables.CtplApprovedAmount) 
                    ? MAX_CTPL_PAY 
                    : Convert.ToDouble(variables.CtplApprovedAmount);
                variables.CTPLApprovedAmount = variables.CtplPayment;
                Console.WriteLine($"CTPL Payment: {variables.CtplPayment}");

                // STD calculations
                variables.StdOrNot = CalculateStdOrNot(variables);
                Console.WriteLine($"STD Or Not: {variables.StdOrNot}");

                // PTO Supplement calculations
                variables.PtoSuppDollars = variables.WeeklyWage - variables.CtplPayment - variables.StdOrNot;
                variables.PtoSuppHrs = variables.PtoSuppDollars / variables.PayRate;
                Console.WriteLine($"PTO Supp Hours: {variables.PtoSuppHrs}");

                // PTO Reserve calculation
                variables.PtoReserve = CalculatePtoReserve(variables);
                Console.WriteLine($"PTO Reserve: {variables.PtoReserve}");

                // PTO Availability calculations
                CalculatePtoAvailability(variables);
                Console.WriteLine($"PTO Availability: {variables.PtoAvailCalc}");

                // Basic Sick calculations
                CalculateBasicSickAvailability(variables);
                Console.WriteLine($"Basic Sick Availability: {variables.BasicSickAvailCalc}");

                Console.WriteLine("Variable calculations completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CalculateVariables: {ex.GetType().Name} - {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                variables = null;
                return false;
            }
        }

        private double CalculateStdOrNot(LeaveVariables variables)
        {
            if (string.IsNullOrEmpty(variables.StdApprovedThrough) || 
                variables.PayEndDate > Convert.ToDateTime(variables.StdApprovedThrough))
            {
                return 0;
            }

            var stdAmount = variables.WeeklyWage * 0.6;
            return stdAmount > variables.CtplPayment ? stdAmount - variables.CtplPayment : 0;
        }

        private double CalculatePtoReserve(LeaveVariables variables)
        {
            return variables.EePtoRtw?.Contains("Y") == true 
                ? variables.ScheduledHours * 2 
                : 0;
        }

        private void CalculatePtoAvailability(LeaveVariables variables)
        {
            var lastWeek1 = !string.IsNullOrEmpty(variables.PtoHrsLast1Week) 
                ? Convert.ToDouble(variables.PtoHrsLast1Week) 
                : 0;
            var lastWeek2 = !string.IsNullOrEmpty(variables.PtoHrsLast2Week) 
                ? Convert.ToDouble(variables.PtoHrsLast2Week) 
                : 0;

            variables.PtoAvailCalc = !string.IsNullOrEmpty(variables.PtoAvail)
                ? Convert.ToDouble(variables.PtoAvail) - (variables.WeekOfPP == 1 ? lastWeek1 + lastWeek2 : lastWeek1)
                : 0;
            
            variables.PtoUsable = Math.Max(0, variables.PtoAvailCalc - variables.PtoReserve);
            variables.PtoUseHrs = variables.PtoUsable - variables.PtoSuppHrs > 0 && variables.PtoSuppHrs > 0 
                ? variables.PtoSuppHrs 
                : 0;
        }

        private void CalculateBasicSickAvailability(LeaveVariables variables)
        {
            var lastWeek1 = !string.IsNullOrEmpty(variables.BasicSickLast1Week) 
                ? Convert.ToDouble(variables.BasicSickLast1Week) 
                : 0;
            var lastWeek2 = !string.IsNullOrEmpty(variables.BasicSickLast2Week) 
                ? Convert.ToDouble(variables.BasicSickLast2Week) 
                : 0;

            variables.BasicSickAvailCalc = !string.IsNullOrEmpty(variables.BasicSickAvail)
                ? Convert.ToDouble(variables.BasicSickAvail) - (variables.WeekOfPP == 1 ? lastWeek1 + lastWeek2 : lastWeek1)
                : 0;
        }
    }
} 