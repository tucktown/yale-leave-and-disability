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

                // Calculate additional variables
                variables.WeeklyWage = variables.PayRate * variables.ScheduledHours;
                
                // CTPL calculations - ADDED FOR CONSISTENCY
                variables.MinWage40 = MIN_WAGE * 40;
                variables.NinetyFiveCTMin40 = variables.MinWage40 * 0.95;
                variables.CtplCalcStar = (variables.WeeklyWage - variables.MinWage40) * 0.6;
                variables.CtplCalc = variables.NinetyFiveCTMin40 + variables.CtplCalcStar;

                // CTPL Payment
                if (!string.IsNullOrEmpty(variables.CtplApprovedAmount))
                {
                    variables.CtplPayment = Convert.ToDouble(variables.CtplApprovedAmount);
                }
                else if (variables.CtplCalc < MAX_CTPL_PAY)
                {
                    variables.CtplPayment = variables.CtplCalc;
                }
                else
                {
                    variables.CtplPayment = MAX_CTPL_PAY;
                }
                variables.CTPLApprovedAmount = variables.CtplPayment;

                // Calculate PtoSuppDollars and PtoSuppHrs early to break dependency
                bool isStdInactiveForDict = string.IsNullOrEmpty(variables.StdApprovedThrough) || 
                                            variables.PayStartDate > DateTime.Parse(variables.StdApprovedThrough);
                variables.PtoSuppDollars = isStdInactiveForDict
                    ? variables.WeeklyWage - variables.CtplPayment
                    : variables.WeeklyWage - variables.CtplPayment - variables.StdOrNot; // Needs StdOrNot, CtplPayment, WeeklyWage
                variables.PtoSuppHrs = variables.PtoSuppDollars / variables.PayRate;
                
                // Additional calculations
                CalculateBasicSickAvailability(variables);
                CalculatePtoAvailability(variables);

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
                
                Console.WriteLine($"BASICSICK_LAST1WEEK: {row["BASICSICK_LAST1WEEK"]}");
                string sickLast1Week = row["BASICSICK_LAST1WEEK"]?.ToString();
                variables.BasicSickLast1Week = string.IsNullOrEmpty(sickLast1Week) ? 0.0 : Convert.ToDouble(sickLast1Week);
                
                Console.WriteLine($"BASICSICK_LAST2WEEK: {row["BASICSICK_LAST2WEEK"]}");
                string sickLast2Week = row["BASICSICK_LAST2WEEK"]?.ToString();
                variables.BasicSickLast2Week = string.IsNullOrEmpty(sickLast2Week) ? 0.0 : Convert.ToDouble(sickLast2Week);
                
                Console.WriteLine($"BASICSICK_AVAILABLE: {row["BASICSICK_AVAILABLE"]}");
                string sickAvail = row["BASICSICK_AVAILABLE"]?.ToString();
                variables.BasicSickAvail = string.IsNullOrEmpty(sickAvail) ? 0.0 : Convert.ToDouble(sickAvail);
                
                Console.WriteLine($"EMP_STATUS: {row["EMP_STATUS"]}");
                variables.EmployeeStatus = row["EMP_STATUS"]?.ToString();

                Console.WriteLine("Starting calculations...");

                // Weekly wage calculations
                variables.WeeklyWage = variables.PayRate * variables.ScheduledHours;
                Console.WriteLine($"WeeklyWage: {variables.WeeklyWage}");
                
                // CTPL calculations
                variables.MinWage40 = MIN_WAGE * 40;
                variables.NinetyFiveCTMin40 = variables.MinWage40 * 0.95;
                variables.CtplCalcStar = (variables.WeeklyWage - variables.MinWage40) * 0.6;
                variables.CtplCalc = variables.NinetyFiveCTMin40 + variables.CtplCalcStar;
                Console.WriteLine($"CTPL Calc: {variables.CtplCalc}");

                // CTPL Payment
                if (!string.IsNullOrEmpty(variables.CtplApprovedAmount))
                {
                    variables.CtplPayment = Convert.ToDouble(variables.CtplApprovedAmount);
                }
                else if (variables.CtplCalc < MAX_CTPL_PAY)
                {
                    variables.CtplPayment = variables.CtplCalc;
                }
                else
                {
                    variables.CtplPayment = MAX_CTPL_PAY;
                }
                variables.CTPLApprovedAmount = variables.CtplPayment;
                Console.WriteLine($"CTPL Payment: {variables.CtplPayment}");

                // STD calculations
                variables.StdOrNot = CalculateStdOrNot(variables);
                Console.WriteLine($"STD Or Not: {variables.StdOrNot}");

                // Calculate PtoSuppDollars and PtoSuppHrs early to break dependency
                bool isStdInactiveForRow = string.IsNullOrEmpty(variables.StdApprovedThrough) || 
                                        variables.PayStartDate > DateTime.Parse(variables.StdApprovedThrough);
                variables.PtoSuppDollars = isStdInactiveForRow
                    ? variables.WeeklyWage - variables.CtplPayment
                    : variables.WeeklyWage - variables.CtplPayment - variables.StdOrNot; // Needs StdOrNot, CtplPayment, WeeklyWage
                variables.PtoSuppHrs = variables.PtoSuppDollars / variables.PayRate;
                Console.WriteLine($"Moved PTO Supp $: {variables.PtoSuppDollars}");
                Console.WriteLine($"Moved PTO Supp Hrs: {variables.PtoSuppHrs}");

                // Basic Sick calculations first due to dependency
                CalculateBasicSickAvailability(variables);

                // PTO Supplement calculations
                CalculatePtoAvailability(variables);

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
            // Calculate PTO reserve based on EE_PTO_RTW (PTO_RESERVE)
            if (variables.EePtoRtw?.Equals("N", StringComparison.OrdinalIgnoreCase) == true)
            {
                variables.PtoReserve = 0;
            }
            else // Handles Y, null, or empty string, or any value other than N
            {
                variables.PtoReserve = variables.ScheduledHours * 2;
            }
            
            // Calculate PTO availability based on WEEK_OF_PP (PTO_AVAIL_CALC)
            variables.PtoAvailCalc = variables.WeekOfPP == 1
                ? variables.PtoAvail - variables.PtoHrsLast1Week - variables.PtoHrsLast2Week
                : variables.PtoAvail - variables.PtoHrsLast1Week;
            
            // Calculate PTO usability based on PTO_AVAIL and PTO_RESERVE (PTO_USABLE)
            variables.PtoUsable = (variables.PtoAvailCalc - variables.PtoReserve) > 0 
                ? (variables.PtoAvailCalc - variables.PtoReserve) 
                : 0;
            
            // Calculate PTO use hours based on PTO_USABLE and PTO_SUPP_HRS (PTO_USE_HRS)
            if ((variables.PtoUsable - variables.PtoSuppHrs) > 0)
            {
                variables.PtoUseHrs = variables.PtoSuppHrs;
            }
            else if (variables.PtoSuppHrs > 0)
            {
                variables.PtoUseHrs = variables.PtoUsable;
            }
            else
            {
                variables.PtoUseHrs = 0;
            }

            // Calculate PTO Basic Sick STD
            double fortyPercentOfScheduledHours = variables.ScheduledHours * 0.4;
            if (variables.PtoUsable >= fortyPercentOfScheduledHours - variables.BasicSickStd)
            {
                variables.PtoBasicSickStd = fortyPercentOfScheduledHours - variables.BasicSickStd;
            }
            else
            {
                variables.PtoBasicSickStd = variables.PtoUsable;
            }
            Console.WriteLine($"PTO Basic Sick STD: {variables.PtoBasicSickStd}");

            // Calculate PTO Basic Sick STD CTPL
            if (variables.PtoUseHrs >= variables.PtoSuppHrs - variables.BasicSickStdCtpl)
            {
                variables.PtoBasicSickStdCtpl = variables.PtoSuppHrs - variables.BasicSickStdCtpl;
            }
            else
            {
                variables.PtoBasicSickStdCtpl = variables.PtoUsable;
            }
            
            Console.WriteLine($"PTO Usable: {variables.PtoUsable}");
            Console.WriteLine($"PTO Use Hrs: {variables.PtoUseHrs}");
            Console.WriteLine($"PTO Supp $: {variables.PtoSuppDollars}");
            Console.WriteLine($"PTO Reserve: {variables.PtoReserve}");
            Console.WriteLine($"PTO Basic Sick STD CTPL: {variables.PtoBasicSickStdCtpl}");
        }
        
        private void CalculateBasicSickAvailability(LeaveVariables variables)
        {
            // Calculate Basic Sick availability
            variables.BasicSickAvailCalc = variables.BasicSickAvail - variables.BasicSickLast1Week - variables.BasicSickLast2Week;
            Console.WriteLine($"Basic Sick Avail Calc: {variables.BasicSickAvailCalc}");

            // Calculate Basic Sick STD
            double fortyPercentOfScheduledHours = variables.ScheduledHours * 0.4;
            if (variables.BasicSickAvailCalc >= fortyPercentOfScheduledHours)
            {
                variables.BasicSickStd = fortyPercentOfScheduledHours;
            }
            else
            {
                variables.BasicSickStd = variables.BasicSickAvailCalc;
            }
            Console.WriteLine($"Basic Sick STD: {variables.BasicSickStd}");

            // Calculate Basic Sick STD CTPL
            if (variables.BasicSickAvailCalc >= variables.PtoSuppHrs)
            {
                variables.BasicSickStdCtpl = variables.PtoSuppHrs;
            }
            else if (variables.BasicSickAvailCalc > 0)
            {
                variables.BasicSickStdCtpl = variables.BasicSickAvailCalc;
            }
            else
            {
                variables.BasicSickStdCtpl = 0;
            }
            Console.WriteLine($"Basic Sick STD CTPL: {variables.BasicSickStdCtpl}");
        }
        
        private double CalculateStdOrNot(LeaveVariables variables)
        {
            // Calculate STD or Not based on weekly wage and CTPL payment
            double stdAmount = variables.WeeklyWage * 0.6;
            return stdAmount > variables.CtplPayment ? (stdAmount - variables.CtplPayment) : 0;
        }
    }
} 