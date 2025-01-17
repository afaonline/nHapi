/*
    The contents of this file are subject to the Mozilla Public License Version 1.1
    (the "License"); you may not use this file except in compliance with the License.
    You may obtain a copy of the License at http://www.mozilla.org/MPL/
    Software distributed under the License is distributed on an "AS IS" basis,
    WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for the
    specific language governing rights and limitations under the License.

    The Original Code is "DT.java".  Description:
    "Note: The class description below has been excerpted from the Hl7 2.3.0 documentation"

    The Initial Developer of the Original Code is University Health Network. Copyright (C)
    2005.  All Rights Reserved.

    Contributor(s): ______________________________________.

    Alternatively, the contents of this file may be used under the terms of the
    GNU General Public License (the "GPL"), in which case the provisions of the GPL are
    applicable instead of those above.  If you wish to allow use of your version of this
    file only under the terms of the GPL and not to allow others to use your version
    of this file under the MPL, indicate your decision by deleting  the provisions above
    and replace  them with the notice and other provisions required by the GPL License.
    If you do not delete the provisions above, a recipient may use your version of
    this file under either the MPL or the GPL.
*/

namespace NHapi.Base.Model.Primitive
{
    using System;

    /// <summary>
    /// Represents an HL7 DT (date) datatype.
    /// </summary>
    /// <author><a href="mailto:neal.acharya@uhn.on.ca">Neal Acharya</a></author>
    /// <author><a href="mailto:bryan.tripp@uhn.on.ca">Bryan Tripp</a></author>
    /// <version>
    /// $Revision: 1.3 $ updated on $Date: 2005/06/08 00:28:25 $ by $Author: bryan_tripp $.
    /// </version>
    public abstract class DT : AbstractPrimitive
    {
        private CommonDT myDetail;

        /// <summary>Construct the type.</summary>
        /// <param name="theMessage">message to which this Type belongs.</param>
        protected DT(IMessage theMessage)
            : base(theMessage)
        {
        }

        /// <summary>Construct the type.</summary>
        /// <param name="theMessage">message to which this Type belongs.</param>
        /// <param name="description">The description of this type.</param>
        protected DT(IMessage theMessage, string description)
            : base(theMessage, description)
        {
        }

        /// <inheritdoc />
        public override string Value
        {
            get
            {
                return (myDetail != null) ? myDetail.Value : base.Value;
            }

            set
            {
                base.Value = value;

                if (myDetail != null)
                {
                    myDetail.Value = value;
                }
            }
        }

        public virtual int YearPrecision
        {
            set { Detail.YearPrecision = value; }
        }

        /// <summary>
        /// Gets the year as an integer.
        /// </summary>
        public virtual int Year => Detail.Year;

        /// <summary>
        /// Gets the month as an integer.
        /// </summary>
        public virtual int Month => Detail.Month;

        /// <summary>
        /// Gets the day as an integer.
        /// </summary>
        public virtual int Day => Detail.Day;

        private CommonDT Detail
        {
            get
            {
                if (myDetail == null)
                {
                    myDetail = new CommonDT(Value);
                }

                return myDetail;
            }
        }

        [Obsolete("This method has been replaced by 'SetYearMonthPrecision'.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "StyleCop.CSharp.NamingRules",
            "SA1300:Element should begin with upper-case letter",
            Justification = "As this is a public member, we will duplicate the method and mark this one as obsolete.")]
        public virtual void setYearMonthPrecision(int yr, int mnth)
        {
            SetYearMonthPrecision(yr, mnth);
        }

        /// <seealso cref="CommonDT.setYearMonthPrecision(int, int)">
        /// </seealso>
        /// <throws>  DataTypeException if the value is incorrectly formatted.  If validation is enabled, this.  </throws>
        /// <summary>      exception should be thrown at setValue(), but if not, detailed parsing may be deferred until
        /// this method is called.
        /// </summary>
        public virtual void SetYearMonthPrecision(int yr, int mnth)
        {
            Detail.SetYearMonthPrecision(yr, mnth);
        }

        [Obsolete("This method has been replaced by 'SetYearMonthDayPrecision'.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "StyleCop.CSharp.NamingRules",
            "SA1300:Element should begin with upper-case letter",
            Justification = "As this is a public member, we will duplicate the method and mark this one as obsolete.")]
        public virtual void setYearMonthDayPrecision(int yr, int mnth, int dy)
        {
            SetYearMonthDayPrecision(yr, mnth, dy);
        }

        /// <seealso cref="CommonDT.setYearMonthDayPrecision(int, int, int)">
        /// </seealso>
        /// <throws>  DataTypeException if the value is incorrectly formatted.  If validation is enabled, this.  </throws>
        /// <summary>      exception should be thrown at setValue(), but if not, detailed parsing may be deferred until
        /// this method is called.
        /// </summary>
        public virtual void SetYearMonthDayPrecision(int yr, int mnth, int dy)
        {
            Detail.SetYearMonthDayPrecision(yr, mnth, dy);
        }
    }
}