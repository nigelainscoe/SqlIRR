// NewtonRaphsonIRRCalculator.cs - Calculate the Internal rate of return for a given set of cashflows.
// Zainco Ltd
//
// See http://zainco.blogspot.com/2008/08/internal-rate-of-return-using-newton.html for background context
//
// Author: Joseph A. Nyirenda <joseph.nyirenda@gmail.com>
//             Mai Kalange<code5p@yahoo.co.uk>
// Copyright (c) 2008 Joseph A. Nyirenda, Mai Kalange, Zainco Ltd
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of version 2 of the GNU General Public
// License as published by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// General Public License for more details.
//
// You should have received a copy of the GNU General Public
// License along with this program; if not, write to the
// Free Software Foundation, Inc., 59 Temple Place - Suite 330,
// Boston, MA 02111-1307, USA.


// SQL Usage:
//  DECLARE @Revenues AS NVARCHAR(MAX) = '-3000, 1850, 1400, 1000'
//  SELECT Finance.SqlIRR(@Revenues)





using System;
using System.Data.SqlTypes;

namespace ClrTest.Functions
{
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once UnusedMember.Global
    public class IRR
    {
        /// <summary>
        /// Takes a string of values and returns the Internal rate of return.
        /// </summary>
        /// <param name="values">a NVARCHAR(MAX) string of numeric values in the form Internal rate of return.</param>
        /// <returns>A value between 0 and 1 indicating the IRR of the financial series passed in.</returns>

        [Microsoft.SqlServer.Server.SqlFunction]

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Global
        public static SqlDouble SqlIRR(string values)
        {
            Array.ConvertAll(values.Split(','), double.Parse);
            double[] cashFlows = Array.ConvertAll(values.Split(','), double.Parse);
            ICalculator calculator = new NewtonRaphsonIrrCalculator(cashFlows);

            return calculator.Execute();

        }
    }

    public class NewtonRaphsonIrrCalculator : ICalculator
    {
        private readonly double[] _cashFlows;
        private int _numberOfIterations;
        private double _result;

        public NewtonRaphsonIrrCalculator(double[] cashFlows)
        {
            _cashFlows = cashFlows;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is valid cash flows.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is valid cash flows; otherwise, <c>false</c>.
        /// </value>
        private bool IsValidCashFlows
        {
            //Cash flows for the first period must be negative
            //There should be at least two cash flow periods         
            get
            {
                const int minNoCashFlowPeriods = 2;

                if (_cashFlows.Length < minNoCashFlowPeriods || (_cashFlows[0] > 0))
                {
                    throw new ArgumentOutOfRangeException(
                        $"Cash flow for the first period  must be negative and there should be at least two periods");
                }
                return true;
            }
        }

        /// <summary>
        /// Gets the initial guess.
        /// </summary>
        /// <value>The initial guess.</value>
        private double InitialGuess
        {
            get
            {
                double initialGuess = -1 * (1 + (_cashFlows[1] / _cashFlows[0]));
                return initialGuess;
            }
        }

        #region ICalculator Members

        public double Execute()
        {
            if (IsValidCashFlows)
            {
                DoNewtonRapshonCalculation(InitialGuess);

                if (_result > 1)
                    throw new IrrCalculationException(
                        "Failed to calculate the IRR for the cash flow series. Please provide a valid cash flow sequence");
            }
            return _result;
        }

        #endregion

        /// <summary>
        /// Does the newton rapshon calculation.
        /// </summary>
        /// <param name="estimatedReturn">The estimated return.</param>
        /// <returns></returns>
        private void DoNewtonRapshonCalculation(double estimatedReturn)
        {
            _numberOfIterations++;
            _result = estimatedReturn - SumOfIrrPolynomial(estimatedReturn) / IrrDerivativeSum(estimatedReturn);
            while (!HasConverged(_result) && ConfigurationHelper.MaxIterations != _numberOfIterations)
            {
                DoNewtonRapshonCalculation(_result);
            }
        }


        /// <summary>
        /// Sums the of IRR polynomial.
        /// </summary>
        /// <param name="estimatedReturnRate">The estimated return rate.</param>
        /// <returns></returns>
        private double SumOfIrrPolynomial(double estimatedReturnRate)
        {
            double sumOfPolynomial = 0;
            if (IsValidIterationBounds(estimatedReturnRate))
                for (int j = 0; j < _cashFlows.Length; j++)
                {
                    sumOfPolynomial += _cashFlows[j] / (Math.Pow((1 + estimatedReturnRate), j));
                }
            return sumOfPolynomial;
        }

        /// <summary>
        /// Determines whether the specified estimated return rate has converged.
        /// </summary>
        /// <param name="estimatedReturnRate">The estimated return rate.</param>
        /// <returns>
        /// 	<c>true</c> if the specified estimated return rate has converged; otherwise, <c>false</c>.
        /// </returns>
        private bool HasConverged(double estimatedReturnRate)
        {
            //Check that the calculated value makes the IRR polynomial zero.
            bool isWithinTolerance = Math.Abs(SumOfIrrPolynomial(estimatedReturnRate)) <= ConfigurationHelper.Tolerance;
            return (isWithinTolerance);
        }

        /// <summary>
        /// IRRs the derivative sum.
        /// </summary>
        /// <param name="estimatedReturnRate">The estimated return rate.</param>
        /// <returns></returns>
        private double IrrDerivativeSum(double estimatedReturnRate)
        {
            double sumOfDerivative = 0;
            if (IsValidIterationBounds(estimatedReturnRate))
                for (int i = 1; i < _cashFlows.Length; i++)
                {
                    sumOfDerivative += _cashFlows[i] * (i) / Math.Pow((1 + estimatedReturnRate), i);
                }
            return sumOfDerivative * -1;
        }

        /// <summary>
        /// Determines whether [is valid iteration bounds] [the specified estimated return rate].
        /// </summary>
        /// <param name="estimatedReturnRate">The estimated return rate.</param>
        /// <returns>
        /// 	<c>true</c> if [is valid iteration bounds] [the specified estimated return rate]; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsValidIterationBounds(double estimatedReturnRate)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return estimatedReturnRate != -1 && (estimatedReturnRate < int.MaxValue) &&
                   (estimatedReturnRate > int.MinValue);
        }
    }

    public static class ConfigurationHelper
    {
        public static readonly int MaxIterations = 50000;

        public static readonly double Tolerance = 0.00000001;
    }

    public class IrrCalculationException : Exception
    {
        public IrrCalculationException(string message) : base(message)
        {
        }
    }
    public interface ICalculator
    {
        /// <summary>
        /// Executes this instance.
        /// </summary>
        /// <returns></returns>
        double Execute();
    }
}