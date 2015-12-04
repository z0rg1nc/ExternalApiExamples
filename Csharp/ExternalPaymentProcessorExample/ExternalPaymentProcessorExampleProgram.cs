using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using NLog;

namespace ExternalPaymentProcessorExample
{
    
    public class ExternalPaymentProcessorExampleOptions
    {
        [Option("payment-type", Required = true)]
        public int PaymentType { get; set; }

        [Option("request-guid", Required = true)]
        public string RequestGuid { get; set; }

        [Option("transfer-guid",Required = true)]
        public string TransferGuid { get; set; }

        [Option("wallet-from", Required = true)]
        public string WalletFrom { get; set; }

        [Option("wallet-to", Required = true)]
        public string WalletTo { get; set; }

        [Option("anonymous", Required = true)]
        public int AnonymousTransfer { get; set; }
        
        [Option("amount", Required = true)]
        public long Amount { get; set; }

        [Option("fee", Required = true)]
        public long Fee { get; set; }

        [Option("comment-bytes-b64", Required = true)]
        public string CommentBytesBase64 { get; set; }

        [Option("sent-time-utc", Required = true)]
        public string SentTimeUtc { get; set; }

        [Option("send-error-code", Required = true)]
        public int SendErrorCode { get; set; }

        [Option("send-error-message", Required = true)]
        public string SendErrorMessage { get; set; }
    }

    public class ExternalPaymentProcessorExampleProgram
    {
        private static readonly Logger _log
            = LogManager.GetCurrentClassLogger();
        public static void Main(string[] args)
        {
            try
            {
                CultureInfo.DefaultThreadCurrentUICulture
                    = CultureInfo.InvariantCulture;
            }
            catch (NotImplementedException)
            {
            }
            /**/
            var options = new ExternalPaymentProcessorExampleOptions();
            try
            {
                CultureInfo provider = CultureInfo.InvariantCulture;
                if (Parser.Default.ParseArguments(args, options))
                {
                    byte[] commentBytes 
                        = Convert.FromBase64String(
                            options.CommentBytesBase64.Trim()
                        );
                    _log.Trace(
                        "Transfer info: {0} {1} {2} {3} {4} {5} {6} {7} \"{8}\" {9} {10} '{11}'",
                        Guid.Parse(options.RequestGuid),
                        Guid.Parse(options.TransferGuid),
                        options.PaymentType,
                        Guid.Parse(options.WalletFrom),
                        Guid.Parse(options.WalletTo),
                        options.AnonymousTransfer == 1 ? "Anonymous" : string.Empty,
                        options.Amount,
                        options.Fee,
                        Convert.ToBase64String(
                            commentBytes
                        ),
                        DateTime.ParseExact(options.SentTimeUtc, "yyyyMMddHHmmss", provider),
                        options.SendErrorCode,
                        options.SendErrorMessage
                    );
                }
                else
                {
                    throw new Exception(
                        "Parse command line args error"
                    );
                }
            }
            catch (Exception exc)
            {
                _log.Error(
                    "Unexpected error '{0}'", 
                    exc.ToString()
                );
            }
        }
    }
}
