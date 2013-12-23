using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Host;
using System.Threading;
using Common.Logging;
using log4net.Repository.Hierarchy;

namespace MMBot
{
    public static class PowershellExtensions
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(PowershellExtensions));

        public static string ExecutePowershellCommand(this string command)
        {
            var host = new MMBotHost();
            using (var runspace = RunspaceFactory.CreateRunspace(host))
            {
                runspace.Open();
                using (var invoker = new RunspaceInvoke(runspace))
                {
                    Collection<PSObject> psObjects;
                    try
                    {
                        IList errors;
                        psObjects = invoker.Invoke(command, null, out errors);
                        if (errors.Count > 0)
                        {
                            string errorString = string.Empty;
                            foreach (var error in errors)
                                errorString += error.ToString();

                            _logger.Error(string.Format("ERROR!: {0}", errorString));
                            psObjects = new Collection<PSObject>
                            {
                                new PSObject(string.Format("Failure running {0}.  {1}.", command, errorString))
                            };
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.Error("ERROR!:", ex);
                        psObjects = new Collection<PSObject>
                        {
                            new PSObject(string.Format("Failure running {0}.  {1}.", command, ex.Message))
                        };
                    }

                    return psObjects.ConvertToString();
                }
            }
        }

        public static string ConvertToString(this Collection<PSObject> psObjects)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var psObject in psObjects)
            {
                _logger.Info(psObject.ImmediateBaseObject.GetType().FullName);
                string message = string.Empty;

                // the PowerShell (.NET) return types we are supporting
                if (psObject.BaseObject.GetType() == typeof(string))
                    message = psObject.ToString();

                else if (psObject.BaseObject.GetType() == typeof(Hashtable))
                {
                    Hashtable hashTable = (Hashtable)psObject.BaseObject;

                    foreach (DictionaryEntry dictionaryEntry in hashTable)
                        message += string.Format("{0} = {1}\n", dictionaryEntry.Key, dictionaryEntry.Value);
                    sb.AppendLine(message);
                }
            }
            return sb.ToString();
        }

    }

    public class MMBotHost : PSHost
    {

        #region Fields

        private Guid m_InstanceId;
        private PSHostUserInterface m_UI;

        #endregion

        #region PSHost Members

        public override System.Globalization.CultureInfo CurrentCulture
        {
            get
            {
                return Thread.CurrentThread.CurrentCulture;
            }
        }

        public override System.Globalization.CultureInfo CurrentUICulture
        {
            get
            {
                return Thread.CurrentThread.CurrentUICulture;
            }
        }

        public override void EnterNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override void ExitNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override Guid InstanceId
        {
            get
            {
                if (m_InstanceId == Guid.Empty)
                {
                    m_InstanceId = Guid.NewGuid();
                }
                return m_InstanceId;
            }
        }

        public override string Name
        {
            get
            {
                return "mmbot";
            }
        }

        public override void NotifyBeginApplication()
        {
            throw new NotImplementedException();
        }

        public override void NotifyEndApplication()
        {
            throw new NotImplementedException();
        }

        public override void SetShouldExit(int exitCode)
        {
            throw new NotImplementedException();
        }

        public override System.Management.Automation.Host.PSHostUserInterface UI
        {
            get
            {
                if (m_UI == null)
                {
                    m_UI = new MMBotPSUserInterface();
                }
                return m_UI;
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0);
            }
        }

        #endregion
    }

    public class MMBotPSUserInterface : PSHostUserInterface
    {

        #region Fields

        private readonly ILog _logger = LogManager.GetLogger(typeof(MMBotHost));

        private PSHostRawUserInterface m_RawUI;

        #endregion

        #region PSHostUserInterface Members

        #region Input Methods

        // it's a bot - we don't support input

        public override Dictionary<string, PSObject> Prompt(string caption, string message, System.Collections.ObjectModel.Collection<FieldDescription> descriptions)
        {
            throw new NotImplementedException();
        }

        public override int PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<ChoiceDescription> choices, int defaultChoice)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            throw new NotImplementedException();
        }

        public override PSHostRawUserInterface RawUI
        {
            get
            {
                if (m_RawUI == null)
                {
                    m_RawUI = new MMBotRawUserInterface();
                }
                return m_RawUI;
            }
        }

        public override string ReadLine()
        {
            throw new NotImplementedException();
        }

        public override System.Security.SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Script Output Methods

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            _logger.Info(value);
        }

        public override void Write(string value)
        {
            this.Write(this.RawUI.ForegroundColor, this.RawUI.BackgroundColor, value);
        }

        public override void WriteLine(string value)
        {
            this.Write(value);
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            this.Write(record.PercentComplete.ToString());
        }

        #endregion

        #region Logging Output Methods

        public override void WriteDebugLine(string message)
        {
            _logger.Debug(message);
        }

        public override void WriteErrorLine(string value)
        {
            _logger.Error(value);
        }

        public override void WriteVerboseLine(string message)
        {
            _logger.Info(message);
        }

        public override void WriteWarningLine(string message)
        {
            _logger.Warn(message);
        }

        #endregion

        #endregion

    }

    public class MMBotRawUserInterface : PSHostRawUserInterface
    {

        #region Fields

        private Size m_BufferSize = new Size(80, 25);
        private ConsoleColor m_BackgroundColor = ConsoleColor.Black;
        private ConsoleColor m_ForegroundColor = ConsoleColor.White;
        private Coordinates m_CursorPosition = new Coordinates(0, 0);
        private int m_CursorSize = 1;

        #endregion

        #region PSHostRawUserInterface Members

        public override ConsoleColor BackgroundColor
        {
            get
            {
                return m_BackgroundColor;
            }
            set
            {
                m_BackgroundColor = value;
            }
        }

        public override Size BufferSize
        {
            get
            {
                return m_BufferSize;
            }
            set
            {
                m_BufferSize = value;
            }
        }

        public override Coordinates CursorPosition
        {
            get
            {
                return m_CursorPosition;
            }
            set
            {
                m_CursorPosition = value;
            }
        }

        public override int CursorSize
        {
            get
            {
                return m_CursorSize;
            }
            set
            {
                m_CursorSize = value;
            }
        }

        public override void FlushInputBuffer()
        {
            throw new NotImplementedException();
        }

        public override ConsoleColor ForegroundColor
        {
            get
            {
                return m_ForegroundColor;
            }
            set
            {
                m_ForegroundColor = value;
            }
        }

        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            throw new NotImplementedException();
        }

        public override bool KeyAvailable
        {
            get { throw new NotImplementedException(); }
        }

        public override Size MaxPhysicalWindowSize
        {
            get { throw new NotImplementedException(); }
        }

        public override Size MaxWindowSize
        {
            get { throw new NotImplementedException(); }
        }

        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            throw new NotImplementedException();
        }

        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            throw new NotImplementedException();
        }

        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            throw new NotImplementedException();
        }

        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            throw new NotImplementedException();
        }

        public override Coordinates WindowPosition
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override Size WindowSize
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override string WindowTitle
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

    }
}
