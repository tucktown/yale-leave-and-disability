using System;
using System.Data;
using System.Collections.Generic;
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

        public bool CalculateVariables(Dictionary<string, object> data, out LeaveVariables variables)
        {
            variables = new LeaveVariables();
            
            try
            {
                Console.WriteLine("Starting variable calculations from dictionary...");

                // Set basic information
                Console.WriteLine($"SCHED_HRS: {data["SCHED_HRS"]}");
                variables.ScheduledHours = Convert.ToDouble(data["SCHED_HRS"]);
                Console.WriteLine($"PAY_RATE: {data["PAY_RATE"]}");
                variables.PayRate = Convert.ToDouble(data["PAY_RATE"]);
                Console.WriteLine($"WEEK_OF_PP: {data["WEEK_OF_PP"]}");
                variables.WeekOfPP = Convert.ToInt32(data["WEEK_OF_PP"]);
                Console.WriteLine($"PAY_START_DATE: {data["PAY_START_DATE"]}");
                variables.PayStartDate = Convert.ToDateTime(data["PAY_START_DATE"]);
                Console.WriteLine($"PAY_END_DATE: {data["PAY_END_DATE"]}");
                variables.PayEndDate = Convert.ToDateTime(data["PAY_END_DATE"]);

                // Set condition-related fields
                if (data.ContainsKey("FMLA_APPR_DATE") && !string.IsNullOrEmpty(data["FMLA_APPR_DATE"]?.ToString()))
                {
                    variables.FmlaApprDate = data["FMLA_APPR_DATE"].ToString();
                }

                if (data.ContainsKey("CTPL_START_DATE") && !string.IsNullOrEmpty(data["CTPL_START_DATE"]?.ToString()))
                {
                    variables.CtplStart = data["CTPL_START_DATE"].ToString();
                }

                if (data.ContainsKey("CTPL_END_DATE") && !string.IsNullOrEmpty(data["CTPL_END_DATE"]?.ToString()))
                {
                    variables.CtplEnd = data["CTPL_END_DATE"].ToString();
                }

                if (data.ContainsKey("CTPL_FORM") && !string.IsNullOrEmpty(data["CTPL_FORM"]?.ToString()))
                {
                    variables.CtplForm = data["CTPL_FORM"].ToString();
                }

                if (data.ContainsKey("STD_APPROVED_THROUGH") && !string.IsNullOrEmpty(data["STD_APPROVED_THROUGH"]?.ToString()))
                {
                    variables.StdApprovedThrough = data["STD_APPROVED_THROUGH"].ToString();
                }

                if (data.ContainsKey("CTPL_APPROVED_AMOUNT") && !string.IsNullOrEmpty(data["CTPL_APPROVED_AMOUNT"]?.ToString()))
                {
                    variables.CtplApprovedAmount = data["CTPL_APPROVED_AMOUNT"].ToString();
                }

                if (data.ContainsKey("EE_PTO_RTW") && !string.IsNullOrEmpty(data["EE_PTO_RTW"]?.ToString()))
                {
                    variables.EePtoRtw = data["EE_PTO_RTW"].ToString();
                }

                if (data.ContainsKey("EMPLOYEE_STATUS") && !string.IsNullOrEmpty(data["EMPLOYEE_STATUS"]?.ToString()))
                {
                    variables.EmployeeStatus = data["EMPLOYEE_STATUS"].ToString();
                }

                // Set PTO and basic sick amounts
                if (data.ContainsKey("PTO_AVAILABLE") && !string.IsNullOrEmpty(data["PTO_AVAILABLE"]?.ToString()))
                {
                    variables.PtoAvail = Convert.ToDouble(data["PTO_AVAILABLE"]);
                }

                if (data.ContainsKey("PTO_LAST1WEEK") && !string.IsNullOrEmpty(data["PTO_LAST1WEEK"]?.ToString()))
                {
                    variables.PtoHrsLast1Week = Convert.ToDouble(data["PTO_LAST1WEEK"]);
                }

                if (data.ContainsKey("PTO_LAST2WEEK") && !string.IsNullOrEmpty(data["PTO_LAST2WEEK"]?.ToString()))
                {
                    variables.PtoHrsLast2Week = Convert.ToDouble(data["PTO_LAST2WEEK"]);
                }

                if (data.ContainsKey("BASICSICK_AVAILABLE") && !string.IsNullOrEmpty(data["BASICSICK_AVAILABLE"]?.ToString()))
                {
                    variables.BasicSickAvail = Convert.ToDouble(data["BASICSICK_AVAILABLE"]);
                }

                if (data.ContainsKey("BASICSICK_LAST1WEEK") && !string.IsNullOrEmpty(data["BASICSICK_LAST1WEEK"]?.ToString()))
                {
                    variables.BasicSickLast1Week = Convert.ToDouble(data["BASICSICK_LAST1WEEK"]);
                }

                if (data.ContainsKey("BASICSICK_LAST2WEEK") && !string.IsNullOrEmpty(data["BASICSICK_LAST2WEEK"]?.ToString()))
                {
                    variables.BasicSickLast2Week = Convert.ToDouble(data["BASICSICK_LAST2WEEK"]);
                }

                // Calculate hours per week
                variables.HoursPerWeek = variables.ScheduledHours;

                // Calculate additional variables
                variables.BasicPay = variables.PayRate * variables.ScheduledHours;
                
                // Additional calculations
                CalculatePtoAvailability(variables);
                CalculateBasicSickAvailability(variables);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CalculateVariables (dictionary): {ex.GetType().Name} - {ex.Message}");
                return false;
            }
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

                Console.WriteLine($"FMLA_APPR_DATE: {row["FMLA_APPR_DATE"]}");
                variables.FmlaApprDate = row["FMLA_APPR_DATE"]?.ToString();
                Console.WriteLine($"CTPL_START_DATE: {row["CTPL_START_DATE"]}");
                variables.CtplStart = row["CTPL_START_DATE"]?.ToString();
                Console.WriteLine($"CTPL_END_DATE: {row["CTPL_END_DATE"]}");
                variables.CtplEnd = row["CTPL_END_DATE"]?.ToString();
                Console.WriteLine($"CTPL_FORM: {row["CTPL_FORM"]}");
                variables.CtplForm = row["CTPL_FORM"]?.ToString();
                Console.WriteLine($"STD_APPROVED_THROUGH: {row["STD_APPROVED_THROUGH"]}");
                variables.StdApprovedThrough = row["STD_APPROVED_THROUGH"]?.ToString();
                Console.WriteLine($"CTPL_APPROVED_AMOUNT: {row["CTPL_APPROVED_AMOUNT"]}");
                variables.CtplApprovedAmount = row["CTPL_APPROVED_AMOUNT"]?.ToString();
                Console.WriteLine($"EE_PTO_RTW: {row["EE_PTO_RTW"]}");
                variables.EePtoRtw = row["EE_PTO_RTW"]?.ToString();
                Console.WriteLine($"PTO_HRS_LASTWEEK: {row["PTO_HRS_LASTWEEK"]}");
                
                // Convert string values to double for numeric fields
                string ptoLast1Week = row["PTO_HRS_LASTWEEK"]?.ToString();
                variables.PtoHrsLast1Week = string.IsNullOrEmpty(ptoLast1Week) ? 0.0 : Convert.ToDouble(ptoLast1Week);
                
                Console.WriteLine($"PTO_HRS_LAST_TWOWEEK: {row["PTO_HRS_LAST_TWOWEEK"]}");
                string ptoLast2Week = row["PTO_HRS_LAST_TWOWEEK"]?.ToString();
                variables.PtoHrsLast2Week = string.IsNullOrEmpty(ptoLast2Week) ? 0.0 : Convert.ToDouble(ptoLast2Week);
                
                Console.WriteLine($"PTO_AVAIL: {row["PTO_AVAIL"]}");
                string ptoAvail = row["PTO_AVAIL"]?.ToString();
                variables.PtoAvail = string.IsNullOrEmpty(ptoAvail) ? 0.0 : Convert.ToDouble(ptoAvail);
                
                Console.WriteLine($"BASIC_SICK_HRS: {row["BASIC_SICK_HRS"]}");
                string sickLast1Week = row["BASIC_SICK_HRS"]?.ToString();
                variables.BasicSickLast1Week = string.IsNullOrEmpty(sickLast1Week) ? 0.0 : Convert.ToDouble(sickLast1Week);
                
                Console.WriteLine($"BRIDGEPORT_SICK_HRS: {row["BRIDGEPORT_SICK_HRS"]}");
                string sickLast2Week = row["BRIDGEPORT_SICK_HRS"]?.ToString();
                variables.BasicSickLast2Week = string.IsNullOrEmpty(sickLast2Week) ? 0.0 : Convert.ToDouble(sickLast2Week);
                
                Console.WriteLine($"BH_SICK_AVAIL: {row["BH_SICK_AVAIL"]}");
                string sickAvail = row["BH_SICK_AVAIL"]?.ToString();
                variables.BasicSickAvail = string.IsNullOrEmpty(sickAvail) ? 0.0 : Convert.ToDouble(sickAvail);
                
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
                
                // Add missing properties
                variables.HoursPerWeek = variables.ScheduledHours;
                variables.BasicPay = variables.PayRate * variables.ScheduledHours;

                // PTO Supplement calculations
                CalculatePtoAvailability(variables);

                // Basic Sick calculations
                CalculateBasicSickAvailability(variables);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CalculateVariables: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }

        private void CalculatePtoAvailability(LeaveVariables variables)
        {
            // Calculate PTO availability
            variables.PtoAvailCalc = variables.PtoAvail - variables.PtoHrsLast1Week - variables.PtoHrsLast2Week;
            
            // Calculate PTO usability
            variables.PtoUsable = variables.PtoAvailCalc > 0 ? variables.PtoAvailCalc : 0;
            
            // Calculate PTO use hours
            variables.PtoUseHrs = variables.WeekOfPP == 1 
                ? Math.Min(variables.ScheduledHours - variables.PtoHrsLast1Week, variables.PtoUsable)
                : Math.Min(variables.ScheduledHours - variables.PtoHrsLast2Week, variables.PtoUsable);
                
            // Calculate PTO supplement dollars
            variables.PtoSuppDollars = variables.PtoUseHrs * variables.PayRate;
            
            // Calculate PTO supplement hours
            variables.PtoSuppHrs = variables.PtoUseHrs;
            
            // Calculate PTO reserve
            variables.PtoReserve = variables.PtoAvail - variables.PtoHrsLast1Week - variables.PtoHrsLast2Week - variables.PtoUseHrs;
            
            Console.WriteLine($"PTO Usable: {variables.PtoUsable}");
            Console.WriteLine($"PTO Use Hrs: {variables.PtoUseHrs}");
            Console.WriteLine($"PTO Supp $: {variables.PtoSuppDollars}");
            Console.WriteLine($"PTO Reserve: {variables.PtoReserve}");
        }
        
        private void CalculateBasicSickAvailability(LeaveVariables variables)
        {
            // Calculate Basic Sick availability
            variables.BasicSickAvailCalc = variables.BasicSickAvail - variables.BasicSickLast1Week - variables.BasicSickLast2Week;
            Console.WriteLine($"Basic Sick Avail Calc: {variables.BasicSickAvailCalc}");
        }
        
        private double CalculateStdOrNot(LeaveVariables variables)
        {
            // Logic for STD or Not calculation
            if (!string.IsNullOrEmpty(variables.StdApprovedThrough))
            {
                return 1.0; // STD Approved
            }
            
            return 0.0; // Not STD Approved
        }
    }
} 