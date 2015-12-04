using System;
using System.Threading.Tasks;
using BtmI2p.GeneralClientInterfaces.WalletServer;
using BtmI2p.MiscUtils;
using Xunit;

/*
DateTime input strings - yyyyMMddHHmmss UTC
throws JsonException
*/

namespace BtmI2p.ExternalAppsLocalApi
{
    public enum EGeneralProxyLocalApiErrorCodes20151004
    {
        NoErrors = 100,
        ProxyNotConnected,
        WrongArgs,
        RequestLifetimeExpired,
        UnknownError
    }

    public static class ProxyLocalJsonRpcApiArgsChecker20151004
    {
        public const string DateTimeStringFormat20151004 = "yyyyMMddHHmmss";
        public class GetInvoiceDataForReplenishment20151004Args
        {
            public long Amount;
        }
        public static void CheckGetInvoiceDataForReplenishment20151004Args(
            GetInvoiceDataForReplenishment20151004Args args)
        {
            try
            {
                Assert.NotNull(args);
                Assert.InRange(
                    args.Amount,
                    1,
                    WalletServerRestrictionsForClient.MaxTransferAmount
                );
            }
            catch
            {
                throw EnumException.Create(
                    EGeneralProxyLocalApiErrorCodes20151004.WrongArgs
                );
            }
        }
    }

    public class GetInvoiceDataForReplenishmentResponse20151004
    {
        public Guid WalletTo;
        public byte[] CommentBytes;
        public long TransferAmount;
        public bool ForceAnonymousTransfer;
    }

    public partial interface IProxyLocalJsonRpcApi
    {
        //EGeneralProxyLocalApiErrorCodes20151004
        Task<bool> IsProxyServerConnected20151004();

        //EGeneralProxyLocalApiErrorCodes20151004
        Task<decimal> GetProxyServerBalance20151004();

        //EGeneralProxyLocalApiErrorCodes20151004
        Task<GetInvoiceDataForReplenishmentResponse20151004> GetInvoiceDataForReplenishment20151004(
            long amount
        );

        //EGeneralProxyLocalApiErrorCodes20151004
        Task<bool> IsNewAppVersionAvailable20151004();

        //EGeneralProxyLocalApiErrorCodes20151004
        Task<string> GetNowLocalTime20151004();

        //EGeneralProxyLocalApiErrorCodes20151004
        Task<string> GetNowServerTime20151004();

        //EGeneralProxyLocalApiErrorCodes20151004
        Task<int> GetServerLocalTimeDiffSeconds20151004();
    }
}
