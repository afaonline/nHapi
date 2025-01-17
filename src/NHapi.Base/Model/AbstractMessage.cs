/*
  The contents of this file are subject to the Mozilla Public License Version 1.1
  (the "License"); you may not use this file except in compliance with the License.
  You may obtain a copy of the License at http://www.mozilla.org/MPL/
  Software distributed under the License is distributed on an "AS IS" basis,
  WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for the
  specific language governing rights and limitations under the License.

  The Original Code is "AbstractMessage.java".  Description:
  "A default implementation of Message"

  The Initial Developer of the Original Code is University Health Network. Copyright (C)
  2001.  All Rights Reserved.

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

namespace NHapi.Base.Model
{
    using System.Text;
    using System.Text.RegularExpressions;

    using NHapi.Base.Parser;
    using NHapi.Base.Validation;

    /// <summary> A default implementation of Message. </summary>
    /// <author>  Bryan Tripp (bryan_tripp@sourceforge.net).
    /// </author>
    public abstract class AbstractMessage : AbstractGroup, IMessage
    {
        /// <param name="theFactory">factory for model classes (e.g. group, segment) for this message.
        /// </param>
        public AbstractMessage(IModelClassFactory theFactory)
            : base(theFactory)
        {
        }

        /// <summary> Returns this Message object - this is an implementation of the
        /// abstract method in AbstractGroup.
        /// </summary>
        public override IMessage Message
        {
            get { return this; }
        }

        /// <summary> Returns the version number.  This default implementation inspects
        /// this.GetClass().getName().  This should be overridden if you are putting
        /// a custom message definition in your own package, or it will default.
        /// </summary>
        /// <returns>s 2.4 if not obvious from package name.
        /// </returns>
        public virtual string Version
        {
            get
            {
                string version = null;

                // TODO: Revisit.
                var p = new Regex("\\.(V2[0-9][0-9]?)\\.");
                var m = p.Match(GetType().FullName);
                if (m.Success)
                {
                    var verFolder = m.Groups[1].Value;
                    if (verFolder.Length > 0)
                    {
                        var chars = verFolder.ToCharArray();
                        var buf = new StringBuilder();
                        for (var i = 1; i < chars.Length; i++)
                        {
                            // start at 1 to avoid the 'v'
                            buf.Append(chars[i]);
                            if (i < chars.Length - 1)
                            {
                                buf.Append('.');
                            }
                        }

                        version = buf.ToString();
                    }
                }

                if (version == null)
                {
                    version = "2.4";
                }

                return version;
            }
        }

        /// <summary>
        /// The validation context.
        /// </summary>
        public virtual IValidationContext ValidationContext { get; set; }
    }
}