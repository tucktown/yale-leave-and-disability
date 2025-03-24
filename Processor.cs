using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;


namespace ESLFeeder
{
    class Processor
    {
        private ODataAccess _dbAccess = new ODataAccess();
        private SDataAccess _sqlAccess = new SDataAccess();
        private ArrayList _current = new ArrayList();

        private double _ScheduledHrs;
        private double _PayRate;

        private double _MinWage = 6.35;
        private double _MaxCtplPay = 981;
        private double _WeeklyWage;
        private double _MinWage40;
        private double _95CTMin40;
        private double _CtplCalcStar;
        private double _CtplCalc;
        private double _CtplPayment;
        private double _StdOrNot;
        private double _PtoSuppDollars;
        private double _PtoSuppHrs;
        private double _PtoUseHrs;
        private double _ptoUsable;
        private double _ptoAvail_Calc;
        private double _ptoReserve = 0;

        private int _week_of_pp;
        private double _BasicSickAvailCalc;

        public void Start()
        {
            DataSet ds = _dbAccess.GetData(new ArrayList() { 0 }, DBConsts.ESL_PACKAGE + DBConsts.GET_ESL_RECORDS);

            if (ds != null)
            {
                if (ds.Tables.Count > 0)
                {
                    // ESL records
                    DataTable dt = ds.Tables[0];

                    if (dt != null)
                    {
                        if (dt.Rows.Count > 0)
                        {
                            _week_of_pp = Convert.ToInt32( dt.Rows[0]["WEEK_OF_PP"]);
                        }
                    }

                    dt = ds.Tables[1];
                    if (dt != null)
                    {
                        if (dt.Rows.Count > 0)
                        {
                            //WorkersCompensation(dt.Select("REASON_CODE = 'WORKERS COMPENSATION'"));

                            Bounding(dt.Select("REASON_CODE = 'BONDING'"));

                        }
                    }
                }  
            }
        }

        private void WorkersCompensation(DataRow[] rows)
        {
            foreach (DataRow row in rows)
            {
                var skip = false;
                _current.Clear();
                _current.Add(row["RECORD_ID"]);

                ArrayList res = new ArrayList() { row["RECORD_ID"] };

                ArrayList par = new ArrayList() { 
                        (int)ESLScenario.WorkersCompensation, 
                        row["CHECK_SEQ"].ToString().Length > 0 ? row["CHECK_SEQ"] : 0 , 
                        row["CLAIM_ID"], 
                        row["PAY_START_DATE"], 
                        row["PAY_END_DATE"], 
                        row["PAY_END_DATE"],
                        0,  //p_AMT_PAID
                        0,  //p_PAID_HRS
                        0,  //p_PTO_HRS
                        0,  //p_SICK_HRS
                        row["SCHED_HRS"],   //p_LOA_NO_HRS_PAID
                        0,0,0,0,0,0,0,0,0

                };

                try
                {
                    if (row["CHECK_SEQ"].ToString() == "")
                    {
                        res.Add((int)ProcessResult.Add);
                    }
                    else
                    {
                        if (row["SCHED_HRS"].ToString() != row["LOA_NO_HRS_PAID"].ToString())
                        {
                            res.Add((int)ProcessResult.Update);
                        }
                        else
                        {
                            res.Add((int)ProcessResult.SkipNoNeedUpdate);
                            skip = true;
                        }
                    }

                    res.Add("");
                    res.Add(0);
                    _dbAccess.ChangeData(res, DBConsts.ESL_PACKAGE + DBConsts.UPDATE_ESL_LOA_PAY_STATUS);

                    if (!skip)
                    {
                        _dbAccess.ChangeData(par, DBConsts.ESL_PACKAGE + DBConsts.ADD_UPDATE_ESL_LOA_PAY);
                    }
                }
                catch (Exception e)
                {
                    _current.Add((int)ProcessResult.Error);
                    _current.Add(e.Message);
                    _current.Add(0);
                    _dbAccess.ChangeData(_current, DBConsts.ESL_PACKAGE + DBConsts.UPDATE_ESL_LOA_PAY_STATUS);
                }
  
            }
        }

        private void Bounding (DataRow[] rows)
        {
            try 
            {
                foreach (DataRow row in rows)
                {
                    var skip = false;
                    _current.Clear();
                    _current.Add(row["RECORD_ID"]);

                    if (ValidVariables(row))
                    {
                        if (CalculateVariables(row))
                        {
                            bool addNew = row["CHECK_SEQ"].ToString() == "" ? true : false;
                            bool hasScenario = false;

                            ArrayList result = new ArrayList() { row["RECORD_ID"] };

                            ArrayList par = new ArrayList() {
                                    (int)ESLScenario.Bonding,
                                    !addNew ? row["CHECK_SEQ"] : 0 ,
                                    row["CLAIM_ID"],
                                    row["PAY_START_DATE"],
                                    row["PAY_END_DATE"],
                                    row["PAY_END_DATE"],
                                    0,  //p_AMT_PAID
                                    0,  //p_PAID_HRS
                                    0,  //p_PTO_HRS
                                    0,  //p_SICK_HRS
                                    0,   //p_LOA_NO_HRS_PAID
                                    0,0,0,0,0,0,0,0,0

                            };

                            
                            bool c7 = Condition7(row);
                            bool c11 = Condition11(row);
                            bool c15 = Condition15(row);
                            bool c17 = Condition17(row);

                            bool c9; bool c10;

                            if (!c11)
                            {
                                c9 = Condition9(row);
                                c10 = Condition10(row);

                                if (c7 && c9 && c10 && !c17)
                                {
                                    if (c15) //Scenario 21
                                    {
                                        hasScenario = true;
                                        par[8] = _PtoUseHrs;
                                        if (addNew)
                                        {
                                            result.Add((int)ProcessResult.Add);
                                            result.Add("Added PTO_HRS: " + _PtoUseHrs.ToString());
                                            result.Add(21);
                                        }
                                        else
                                        {
                                            if (row["PTO_HRS"].ToString() != _PtoUseHrs.ToString())
                                            {
                                                result.Add((int)ProcessResult.Update);
                                                result.Add("PTO_HRS Update from " + row["PTO_HRS"].ToString() + " to " + _PtoUseHrs.ToString());
                                                result.Add(21);
                                            }
                                            else
                                            {
                                                result.Add((int)ProcessResult.SkipNoNeedUpdate);
                                                result.Add("");
                                                result.Add(21);
                                                skip = true;
                                            }
                                        }
                                    }
                                    else //Scenario 22
                                    {
                                        hasScenario = true;
                                        par[10] = _ScheduledHrs;
                                        if (addNew)
                                        {
                                            result.Add((int)ProcessResult.Add);
                                            result.Add("Added LOA_NO_HRS_PAID: " + _ScheduledHrs.ToString());
                                            result.Add(22);
                                        }
                                        else
                                        {
                                            if (row["LOA_NO_HRS_PAID"].ToString() != _ScheduledHrs.ToString())
                                            {
                                                result.Add((int)ProcessResult.Update);
                                                result.Add("LOA_NO_HRS_PAID Update from " + row["LOA_NO_HRS_PAID"].ToString() + " to " + _ScheduledHrs.ToString());
                                                result.Add(22);
                                            }
                                            else
                                            {
                                                result.Add((int)ProcessResult.SkipNoNeedUpdate);
                                                result.Add("");
                                                result.Add(22);
                                                skip = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (c7 && !c17)
                                {
                                    if (c15) //Scenario 23
                                    {
                                        hasScenario = true;
                                        par[8] = _ScheduledHrs;
                                        if (addNew)
                                        {
                                            result.Add((int)ProcessResult.Add);
                                            result.Add("Added PTO_HRS: " + _ScheduledHrs.ToString());
                                            result.Add(23);
                                        }
                                        else
                                        {
                                            if (row["PTO_HRS"].ToString() != _ScheduledHrs.ToString())
                                            {
                                                result.Add((int)ProcessResult.Update);
                                                result.Add("PTO_HRS Update from " + row["PTO_HRS"].ToString() + " to " + _ScheduledHrs.ToString());
                                                result.Add(23);
                                            }
                                            else
                                            {
                                                result.Add((int)ProcessResult.SkipNoNeedUpdate);
                                                result.Add("");
                                                result.Add(23);
                                                skip = true;
                                            }
                                        }
                                    }
                                    else //Scenario 24
                                    {
                                        hasScenario = true;
                                        par[10] = _ScheduledHrs;
                                        if (addNew)
                                        {
                                            result.Add((int)ProcessResult.Add);
                                            result.Add("Added LOA_NO_HRS_PAID: " + _ScheduledHrs.ToString());
                                            result.Add(24);
                                        }
                                        else
                                        {
                                            if (row["LOA_NO_HRS_PAID"].ToString() != _ScheduledHrs.ToString())
                                            {
                                                result.Add((int)ProcessResult.Update);
                                                result.Add("LOA_NO_HRS_PAID Update from " + row["LOA_NO_HRS_PAID"].ToString() + " to " + _ScheduledHrs.ToString());
                                                result.Add(24);
                                            }
                                            else
                                            {
                                                result.Add((int)ProcessResult.SkipNoNeedUpdate);
                                                result.Add("");
                                                result.Add(24);
                                                skip = true;
                                            }
                                        }
                                    }
                                }
                            }

                            //If has a matched case
                            if (hasScenario)
                            {
                                _dbAccess.ChangeData(result, DBConsts.ESL_PACKAGE + DBConsts.UPDATE_ESL_LOA_PAY_STATUS);

                                if (!skip)
                                {
                                    _dbAccess.ChangeData(par, DBConsts.ESL_PACKAGE + DBConsts.ADD_UPDATE_ESL_LOA_PAY);
                                }
                            }
                            else
                            {
                                result.Add((int)ProcessResult.NoMatchScenario);
                                result.Add("No Matching Scenario. C7=" + c7 + " C11=" + c11 + " C15=" + c15 + " C17=" + c17);
                                result.Add(0);
                                _dbAccess.ChangeData(result, DBConsts.ESL_PACKAGE + DBConsts.UPDATE_ESL_LOA_PAY_STATUS);
                            }
                        }
                    }
                    
                }
            }
            catch (Exception e)
            {
                _current.Add((int)ProcessResult.Error);
                _current.Add("From Bounding " + e.Message);
                _current.Add(0);
                _dbAccess.ChangeData(_current, DBConsts.ESL_PACKAGE + DBConsts.UPDATE_ESL_LOA_PAY_STATUS);
            }
            
        }

        private bool ValidVariables(DataRow row)
        {
            ArrayList var = new ArrayList() { row["RECORD_ID"] };

            if (row["SCHED_HRS"].ToString() == "")
            {
                var.Add((int)ProcessResult.Error);
                var.Add("Invalid SCHED_HRS value");
                var.Add(0);
                _dbAccess.ChangeData(var, DBConsts.ESL_PACKAGE + DBConsts.UPDATE_ESL_LOA_PAY_STATUS);

                return false;
            }
            else
                _ScheduledHrs = Convert.ToInt32(row["SCHED_HRS"]);


            if (row["PAY_RATE"].ToString() == "")
            {
                var.Add((int)ProcessResult.Error);
                var.Add("Invalid PAY_RATE value");
                var.Add(0);
                _dbAccess.ChangeData(var, DBConsts.ESL_PACKAGE + DBConsts.UPDATE_ESL_LOA_PAY_STATUS);

                return false;
            }
            else
                _PayRate = Convert.ToInt32(row["PAY_RATE"]);

            return true;
        }

        private bool CalculateVariables(DataRow row)
        {
            try
            {
                _WeeklyWage = _PayRate * _ScheduledHrs;
                _MinWage40 = _MinWage * 40;
                _95CTMin40 = _MinWage40 * 0.95;
                _CtplCalcStar = (_WeeklyWage - _MinWage40) * 0.6;
                _CtplCalc = _95CTMin40 + _CtplCalcStar;

                //CTPL_PAYMENT
                if (row["CTPL_APPROVED_AMOUNT"].ToString().Length == 0)
                {
                    _CtplPayment = _MaxCtplPay;
                }
                else
                {
                    _CtplPayment = Convert.ToInt32(row["CTPL_APPROVED_AMOUNT"]);
                }

                //STD_OR_NOT
                if (row["STD_APPROVED_THROUGH"].ToString().Length == 0 || Convert.ToDateTime(row["PAY_END_DATE"]) > Convert.ToDateTime(row["STD_APPROVED_THROUGH"]))
                {
                    _StdOrNot = 0;
                }
                else
                {
                    if (_WeeklyWage * 0.6 > _CtplPayment)
                        _StdOrNot = _WeeklyWage * 0.6 - _CtplPayment;
                    else
                        _StdOrNot = 0;
                }

                //PTO_SUPP_DOLLARS
                _PtoSuppDollars = _WeeklyWage - _CtplPayment - _StdOrNot;
                //PTO_SUPP_HRS
                _PtoSuppHrs = _PtoSuppDollars / _PayRate;


                //PTO_RESERVE
                if (row["EE_PTO_RTW"].ToString().Contains("Y"))
                {
                    _ptoReserve = Convert.ToInt32(row["SCHED_HRS"]) * 2;
                }
                else
                {
                    _ptoReserve = 0;
                }

                var lw1 = row["PTO_HRS_LAST1WEEK"].ToString().Length > 0 ? Convert.ToInt32(row["PTO_HRS_LAST1WEEK"]) : 0;
                var lw2 = row["PTO_HRS_LAST2WEEK"].ToString().Length > 0 ? Convert.ToInt32(row["PTO_HRS_LAST2WEEK"]) : 0;

                //PTO_AVAIL_CALC
                if (_week_of_pp == 1)
                {
                    _ptoAvail_Calc = Convert.ToInt32(row["PTO_AVAIL"]) - (lw1 + lw2);
                }
                else
                {
                    _ptoAvail_Calc = Convert.ToInt32(row["PTO_AVAIL"]) - lw1;
                }

                //PTO_USABLE
                if (_ptoAvail_Calc - _ptoReserve > 0)
                {
                    _ptoUsable = _ptoAvail_Calc - _ptoReserve;
                }
                else
                    _ptoUsable = 0;

                //PTO_USE_HRS
                if (_ptoUsable - _PtoSuppHrs > 0 && _PtoSuppHrs > 0)
                    _PtoUseHrs = _PtoSuppHrs;
                else
                    _PtoUseHrs = 0;

                //BASIC_SICK_AVAIL_CALC
                if (_week_of_pp == 1)
                {

                }

                return true;
            }
            catch (Exception e)
            {
                _current.Add((int)ProcessResult.Error);
                _current.Add("From CalculateVariables " + e.Message);
                _current.Add(0);
                _dbAccess.ChangeData(_current, DBConsts.ESL_PACKAGE + DBConsts.UPDATE_ESL_LOA_PAY_STATUS);
            }
            return false;
        }

        private bool Condition7(DataRow row)
        {
            if (row["STD_APPROVED_THROUGH"].ToString() == "")
                return true;
            if (Convert.ToDateTime(row["PAY_START_DATE"]) > Convert.ToDateTime(row["STD_APPROVED_THROUGH"]))
                return true;

            return false;
        }

        private bool Condition8(DataRow row)
        {
            if (_StdOrNot / _PayRate > 0)
                return true;

            return false;
        }

        private bool Condition9(DataRow row)
        {
            if (row["CTPL_START"].ToString() == "")
                return true;
            if (Convert.ToDateTime(row["PAY_START_DATE"]) >= Convert.ToDateTime(row["CTPL_START"]))
                return true;

            return false;
        }

        private bool Condition10(DataRow row)
        {
            if (row["CTPL_FORM"].ToString() == "")
                return true;
            if (Convert.ToDateTime(row["PAY_START_DATE"]) <= Convert.ToDateTime(row["CTPL_END"]))
                return true;

            return false;
        }

        private bool Condition11(DataRow row)
        {
            if (row["CTPL_FORM"].ToString() == "")
                return true;
            if (Convert.ToDateTime(row["PAY_START_DATE"]) > Convert.ToDateTime(row["CTPL_END"]))
                return true;

            return false;
        }

        private bool Condition13(DataRow row)
        {
            
            if (_ScheduledHrs * 0.4 <= _ptoUsable)
                return true;

            return false;
        }

        private bool Condition14(DataRow row)
        {

            if (_PtoUseHrs > 0)
                return true;

            return false;
        }

        private bool Condition15(DataRow row)
        {
            if (_ptoUsable > 0)
                return true;
            
            return false;
        }

        private bool Condition16(DataRow row)
        {
            if (Convert.ToDateTime(row["PAY_END_DATE"]) <= Convert.ToDateTime(row["FMLA_APPR_DATE"]))
                return true;

            return false;
        }

        private bool Condition17(DataRow row)
        {
            var c1 = false;
            var c2 = false; ;

            if (row["FMLA_APPR_DATE"].ToString() == "" || Convert.ToDateTime(row["PAY_START_DATE"]) > Convert.ToDateTime(row["FMLA_APPR_DATE"]))
                c1 = true;

            if (row["CTPL_FORM"].ToString() == "" || Convert.ToDateTime(row["PAY_START_DATE"]) > Convert.ToDateTime(row["CTPL_END"]))
                c2 = true;

            if (c1 && c2)
                return true;

            return false;
        }
    }
    
}
