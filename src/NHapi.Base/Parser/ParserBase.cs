/*
  The contents of this file are subject to the Mozilla Public License Version 1.1
  (the "License"); you may not use this file except in compliance with the License.
  You may obtain a copy of the License at http://www.mozilla.org/MPL/
  Software distributed under the License is distributed on an "AS IS" basis,
  WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for the
  specific language governing rights and limitations under the License.

  The Original Code is "Parser.java".  Description:
  "Parses HL7 message Strings into HL7 Message objects and
  encodes HL7 Message objects into HL7 message Strings"

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

namespace NHapi.Base.Parser
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.IO;

    using NHapi.Base.Log;
    using NHapi.Base.Model;
    using NHapi.Base.Validation;
    using NHapi.Base.Validation.Implementation;

    /// <summary>
    /// Parses HL7 message Strings into HL7 Message objects and
    /// encodes HL7 Message objects into HL7 message Strings.
    /// </summary>
    /// <author>Bryan Tripp (bryan_tripp@sourceforge.net).</author>
    public abstract class ParserBase
    {
        private static readonly IHapiLog Log;

        private IValidationContext validationContext;
        private MessageValidator messageValidator;

        static ParserBase()
        {
            Log = HapiLogFactory.GetHapiLog(typeof(ParserBase));
        }

        /// <summary> Uses DefaultModelClassFactory for model class lookup. </summary>
        public ParserBase()
            : this(new DefaultModelClassFactory())
        {
        }

        /// <param name="theFactory">custom factory to use for model class lookup.
        /// </param>
        public ParserBase(IModelClassFactory theFactory)
        {
            Factory = theFactory;
            ValidationContext = new DefaultValidation();
        }

        /// <summary>
        /// Gets the factory used by this Parser for model class lookup.
        /// </summary>
        public virtual IModelClassFactory Factory { get; }

        /// <summary>
        /// Gets or sets the set of validation rules that is applied to messages parsed or encoded by this parser.
        /// </summary>
        public virtual IValidationContext ValidationContext
        {
            get
            {
                return validationContext;
            }

            set
            {
                validationContext = value;
                messageValidator = new MessageValidator(value, true);
            }
        }

        /// <summary>
        /// Gets the preferred encoding of this Parser.
        /// </summary>
        public abstract string DefaultEncoding { get; }

        /// <summary>
        /// Returns event->structure maps.
        /// </summary>
        private static IDictionary MessageStructures => EventMapper.Instance.Maps;

        /// <summary>
        /// Creates a version-specific MSH object and returns it as a version-independent
        /// MSH interface.
        /// throws HL7Exception if there is a problem, e.g. invalid version, code not available
        /// for given version.
        /// </summary>
        public static ISegment MakeControlMSH(string version, IModelClassFactory factory)
        {
            ISegment msh;
            try
            {
                var dummy =
                    (IMessage)GenericMessage
                        .GetGenericMessageClass(version)
                        .GetConstructor(new Type[] { typeof(IModelClassFactory) })
                        .Invoke(new object[] { factory });

                var constructorParamTypes = new Type[] { typeof(IGroup), typeof(IModelClassFactory) };
                var constructorParamArgs = new object[] { dummy, factory };
                var c = factory.GetSegmentClass("MSH", version);
                var constructor = c.GetConstructor(constructorParamTypes);
                msh = (ISegment)constructor.Invoke(constructorParamArgs);
            }
            catch (Exception e)
            {
                throw new HL7Exception(
                    $"Couldn't create MSH for version {version} (does your class path include this version?) ... ",
                    ErrorCode.APPLICATION_INTERNAL_ERROR,
                    e);
            }

            return msh;
        }

        /// <summary> Returns true if the given string represents a valid 2.x version.  Valid versions
        /// include "2.0", "2.0D", "2.1", "2.2", "2.3", "2.3.1", "2.4", "2.5".
        /// </summary>
        public static bool ValidVersion(string version)
        {
            return PackageManager.Instance.IsValidVersion(version);
        }

        /// <summary>
        /// Given a concatenation of message type and event (e.g. ADT_A04), and the
        /// version, finds the corresponding message structure (e.g. ADT_A01).  This
        /// is needed because some events share message structures, although it is not needed
        /// when the message structure is explicitly valued in MSH-9-3.
        /// If no mapping is found, returns the original name.
        /// </summary>
        /// <exception cref="HL7Exception">Thrown if there is an error retrieving the map, or if the given version is invalid.</exception>
        public static string GetMessageStructureForEvent(string name, string version)
        {
            if (!ValidVersion(version))
            {
                throw new HL7Exception($"The version {version} is unknown");
            }

            NameValueCollection p;
            try
            {
                p = (NameValueCollection)MessageStructures[version];
            }
            catch (IOException ioe)
            {
                throw new HL7Exception("Unable to access message structures", ioe);
            }

            if (p == null)
            {
                throw new HL7Exception($"No map found for version {version}");
            }

            var structure = p.Get(name);
            if (structure == null)
            {
                structure = name;
            }

            return structure;
        }

        /// <summary>
        /// Parses a message string and returns the corresponding Message object.
        /// </summary>
        /// <param name="message">a String that contains an HL7 message.</param>
        /// <returns>A HAPI Message object parsed from the given String.</returns>
        /// <exception cref="HL7Exception">if the message is not correctly formatted.</exception>
        /// <exception cref="EncodingNotSupportedException">Thrown if the message encoded is not supported by this parser.</exception>
        public virtual IMessage Parse(string message)
        {
            var version = GetVersion(message);
            if (!ValidVersion(version))
            {
                throw new HL7Exception(
                    $"Can't process message of version '{version}' - version not recognized",
                    ErrorCode.UNSUPPORTED_VERSION_ID);
            }

            return Parse(message, version);
        }

        /// <summary>
        /// Parse a message to a specific assembly.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public virtual IMessage Parse(string message, string version)
        {
            var encoding = GetEncoding(message);
            if (!SupportsEncoding(encoding))
            {
                throw new EncodingNotSupportedException(
                    $"Can't parse message beginning {message.Substring(0, Math.Min(message.Length, 50) - 0)}");
            }

            messageValidator.Validate(message, encoding.Equals("XML"), version);
            var result = DoParse(message, version);
            messageValidator.Validate(result);

            return result;
        }

        /// <summary>
        /// Formats a Message object into an HL7 message string using the given encoding.
        /// </summary>
        /// <param name="source">a Message object from which to construct an encoded message string.</param>
        /// <param name="encoding">the name of the HL7 encoding to use (eg "XML"; most implementations support only
        /// one encoding).
        /// </param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="HL7Exception">Thrown if the data fields in the message do not permit encoding (e.g. required fields are null).</exception>
        /// <exception cref="EncodingNotSupportedException">Thrown if the requested encoding is not supported by this parser.</exception>
        public virtual string Encode(IMessage source, string encoding)
        {
            messageValidator.Validate(source);
            var result = DoEncode(source, encoding);
            messageValidator.Validate(result, encoding.Equals("XML"), source.Version);

            return result;
        }

        /// <summary>
        /// Formats a Message object into an HL7 message string using this parser's
        /// default encoding.
        /// </summary>
        /// <param name="source">
        /// A Message object from which to construct an encoded message string.
        /// </param>
        /// <returns>The encoded message.</returns>
        public virtual string Encode(IMessage source)
        {
            var encoding = DefaultEncoding;

            messageValidator.Validate(source);
            var result = DoEncode(source);
            messageValidator.Validate(result, encoding.Equals("XML"), source.Version);

            return result;
        }

        /// <summary>
        /// Returns a String representing the encoding of the given message, if
        /// the encoding is recognized.  For example if the given message appears
        /// to be encoded using HL7 2.x XML rules then "XML" would be returned.
        /// If the encoding is not recognized then null is returned.  That this
        /// method returns a specific encoding does not guarantee that the
        /// message is correctly encoded (e.g. well formed XML) - just that
        /// it is not encoded using any other encoding than the one returned.
        /// Returns null if the encoding is not recognized.
        /// </summary>
        public abstract string GetEncoding(string message);

        /// <summary>
        /// Returns true if and only if the given encoding is supported
        /// by this Parser.
        /// </summary>
        public abstract bool SupportsEncoding(string encoding);

        /// <summary>
        /// Returns a minimal amount of data from a message string, including only the
        /// data needed to send a response to the remote system.  This includes the
        /// following fields:
        /// <para>
        /// <ul>
        /// <li>field separator</li>
        /// <li>encoding characters</li>
        /// <li>processing ID</li>
        /// <li>message control ID</li>
        /// </ul>
        /// </para>
        /// <para>
        /// This method is intended for use when there is an error parsing a message,
        /// (so the Message object is unavailable) but an error message must be sent
        /// back to the remote system including some of the information in the inbound
        /// message.  This method parses only that required information, hopefully
        /// avoiding the condition that caused the original error.
        /// </para>
        /// </summary>
        /// <returns>An MSH segment.</returns>
        public abstract ISegment GetCriticalResponseData(string message);

        /// <summary>
        /// For response messages, returns the value of MSA-2 (the message ID of the message
        /// sent by the sending system).  This value may be needed prior to main message parsing,
        /// so that (particularly in a multi-threaded scenario) the message can be routed to
        /// the thread that sent the request.  We need this information first so that any
        /// parse exceptions are thrown to the correct thread.  Implementers of Parsers should
        /// take care to make the implementation of this method very fast and robust.
        /// Returns null if MSA-2 can not be found (e.g. if the message is not a
        /// response message).
        /// </summary>
        public abstract string GetAckID(string message);

        /// <summary>
        /// Returns the version ID (MSH-12) from the given message, without fully parsing the message.
        /// The version is needed prior to parsing in order to determine the message class
        /// into which the text of the message should be parsed.
        /// </summary>
        /// <exception cref="HL7Exception">Thrown if the version field can not be found.</exception>
        public abstract string GetVersion(string message);

        /// <summary>
        /// Called by encode(Message, String) to perform implementation-specific encoding work.
        /// </summary>
        /// <param name="source">a Message object from which to construct an encoded message string.</param>
        /// <param name="encoding">the name of the HL7 encoding to use (eg "XML"; most implementations support only one encoding).</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="HL7Exception">Thrown if the data fields in the message do not permit encoding (e.g. required fields are null).</exception>
        /// <exception cref="EncodingNotSupportedException">Thrown if the requested encoding is not supported by this parser.</exception>
        protected internal abstract string DoEncode(IMessage source, string encoding);

        /// <summary>
        /// Called by encode(Message) to perform implementation-specific encoding work.
        /// </summary>
        /// <param name="source">a Message object from which to construct an encoded message string.</param>
        /// <returns>The encoded message.</returns>
        /// <exception cref="HL7Exception">Thrown if the data fields in the message do not permit encoding (e.g. required fields are null).</exception>
        /// <exception cref="EncodingNotSupportedException">Thrown if the requested encoding is not supported by this parser.</exception>
        protected internal abstract string DoEncode(IMessage source);

        /// <summary>
        /// Called by parse() to perform implementation-specific parsing work.
        /// </summary>
        /// <param name="message">a String that contains an HL7 message.</param>
        /// <param name="version">the name of the HL7 version to which the message belongs (eg "2.5").</param>
        /// <returns>A HAPI Message object parsed from the given String.</returns>
        /// <exception cref="HL7Exception">Thrown if the data fields in the message do not permit encoding (e.g. required fields are null).</exception>
        /// <exception cref="EncodingNotSupportedException">Thrown if the requested encoding is not supported by this parser.</exception>
        protected internal abstract IMessage DoParse(string message, string version);

        /// <summary>
        /// Note that the validation context of the resulting message is set to this parser's validation
        /// context. The validation context is used within Primitive.setValue().
        /// </summary>
        /// <param name="theName">name of the desired structure in the form XXX_YYY.</param>
        /// <param name="theVersion">HL7 version (e.g. "2.3").</param>
        /// <param name="isExplicit">
        /// true if the structure was specified explicitly in MSH-9-3, false if it
        /// was inferred from MSH-9-1 and MSH-9-2.  If false, a lookup may be performed to find
        /// an alternate structure corresponding to that message type and event.
        /// </param>
        /// <returns>a Message instance.</returns>
        /// <exception cref="HL7Exception">Thrown when the version is not recognized or no appropriate class can be found or the Message.</exception>
        protected internal virtual IMessage InstantiateMessage(string theName, string theVersion, bool isExplicit)
        {
            var messageClass = Factory.GetMessageClass(theName, theVersion, isExplicit);
            if (messageClass == null)
            {
                throw new Exception($"Can't find message class in current package list: {theName}");
            }

            Log.Info($"Instantiating msg of class {messageClass.FullName}");
            var constructor = messageClass.GetConstructor(new Type[] { typeof(IModelClassFactory) });
            var result = (IMessage)constructor.Invoke(new object[] { Factory });
            result.ValidationContext = validationContext;
            return result;
        }
    }
}