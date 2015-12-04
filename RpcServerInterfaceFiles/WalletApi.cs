using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BtmI2p.AesHelper;
using BtmI2p.GeneralClientInterfaces.WalletServer;
using BtmI2p.MiscUtil.Conversion;
using BtmI2p.MiscUtil.IO;
using BtmI2p.MiscUtils;
using Xunit;

/*
DateTime input strings - yyyyMMddHHmmss UTC
throws JsonException
*/

namespace BtmI2p.ExternalAppsLocalApi
{
    public enum EGeneralWalletLocalApiErrorCodes20151004
    {
        NoErrors = 100,
        WalletNotAllowed,
        WalletNotConnected,
        WrongArgs,
        RequestLifetimeExpired,
        ObsoleteMethod,
        InQueueRetryLater,
        RpcServerDisabled,
        CursorNotFound,
        WrongAuthCode,
        UnknownError
    }
    /**/
    public static class WalletLocalJsonRpcApiArgsChecker20151004
    {
        public const string DateTimeStringFormat20151004 = "yyyyMMddHHmmss";
        public class SendTransfer20151004Args
        {
            public Guid RequestGuid;
            public Guid WalletFromGuid;
            public Guid WalletToGuid;
            public long Amount;
            public string SentTimeString;
            public bool AnonymousTransfer;
            public byte[] CommentBytes;
            public long MaxFee;
            public byte[] HmacAuthCode;
        }
        public static byte[] GetSentTransfer20151004ArgsHmacAuthCode(
            SendTransfer20151004Args request,
            byte[] hmacKey
        )
        {
            Assert.NotNull(request);
            CheckSentTransfer20151004Args(request);
            Assert.NotNull(hmacKey);
            Assert.Equal(hmacKey.Length, 64);
            using (var ms = new MemoryStream())
            {
                var converter = new LittleEndianBitConverter();
                using (var littleStream = new EndianBinaryWriter(converter, ms))
                {
                    littleStream.Write(request.RequestGuid.ToByteArray());
                    var dt = DateTime.ParseExact(
                        request.SentTimeString, 
                        DateTimeStringFormat20151004,
                        CultureInfo.InvariantCulture
                    );
                    littleStream.Write(dt.Ticks);
                    littleStream.Write(request.WalletFromGuid.ToByteArray());
                    littleStream.Write(request.WalletToGuid.ToByteArray());
                    littleStream.Write(request.Amount);
                    littleStream.Write(request.AnonymousTransfer);
                    littleStream.Write(request.CommentBytes);
                    littleStream.Write(request.MaxFee);
                }
                return new HMACSHA256(hmacKey).ComputeHash(ms.ToArray());
            }
        }
        public static void CheckSentTransfer20151004Args(
            SendTransfer20151004Args args
        )
        {
            DateTime dt;
            try
            {
                Assert.NotNull(args);
                Assert.NotEqual(args.WalletFromGuid,Guid.Empty);
                Assert.NotEqual(args.WalletToGuid,Guid.Empty);
                Assert.InRange(
                    args.Amount,
                    1,
                    int.MaxValue
                );
                Assert.NotNull(args.CommentBytes);
                Assert.True(args.CommentBytes.Length <= 64000);
                Assert.False(string.IsNullOrWhiteSpace(args.SentTimeString));
                dt = DateTime.ParseExact(
                    args.SentTimeString, 
                    DateTimeStringFormat20151004, 
                    CultureInfo.InvariantCulture
                );
                Assert.InRange(
                    args.MaxFee,
                    0,
                    int.MaxValue
                );
            }
            catch
            {
                throw EnumException.Create(
                    EGeneralWalletLocalApiErrorCodes20151004.WrongArgs);
            }
            try
            {
                var nowTimeUtc = DateTime.UtcNow;
                Assert.InRange(
                    dt,
                    nowTimeUtc.Subtract(TimeSpan.FromMinutes(5.0)),
                    nowTimeUtc.AddMinutes(5)
                );
            }
            catch
            {
                throw EnumException.Create(
                    EGeneralWalletLocalApiErrorCodes20151004.RequestLifetimeExpired);
            }
        }
        /**/
        public class EstimateFee20151004Args
        {
            public Guid WalletFrom;
            public Guid WalletTo;
            public long Amount;
            public int CommentBytesLength;
            public bool AnonymousTransfer;
        }

        public static void CheckEstimateFee20151004Args(
            EstimateFee20151004Args args
        )
        {
            try
            {
                Assert.NotNull(args);
                Assert.InRange(
                    args.Amount,
                    1,
                    WalletServerRestrictionsForClient.MaxTransferAmount
                );
                Assert.True(args.CommentBytesLength >= 0);
                var encryptedCommentLength = AesKeyIvPair.GenAesKeyIvPair().EncryptData(
                    new byte[args.CommentBytesLength]
                ).Length;
                Assert.InRange(
                    encryptedCommentLength,
                    0,
                    WalletServerRestrictionsForClient.MaxCommentBytesCount
                );
            }
            catch
            {
                throw EnumException.Create(
                    EGeneralWalletLocalApiErrorCodes20151004.WrongArgs);
            }
        }
        /**/

        public class CreateTransferDataCursor20151004Args
        {
            public string DateTimeFrom;
            public string DateTimeTo;
            public bool FetchSentTransfers;
            public bool FetchReceivedTransfers;
            public bool StayOnline;
        }

        public static void CheckCreateTransferDataCursor20151004Args(
            CreateTransferDataCursor20151004Args args)
        {
            try
            {
                Assert.False(string.IsNullOrWhiteSpace(args.DateTimeFrom));
                Assert.False(string.IsNullOrWhiteSpace(args.DateTimeTo));
                var dtFrom = DateTime.ParseExact(
                    args.DateTimeFrom,
                    DateTimeStringFormat20151004,
                    CultureInfo.InvariantCulture
                );
                Assert.InRange(
                    dtFrom,
                    new DateTime(2000,1,1), 
                    new DateTime(3000,1,1)
                );
                var dtTo = DateTime.ParseExact(
                    args.DateTimeTo,
                    DateTimeStringFormat20151004,
                    CultureInfo.InvariantCulture
                );
                Assert.InRange(
                    dtTo,
                    new DateTime(2000,1,1),
                    new DateTime(3000,1,1)
                );
                Assert.True(dtTo >= dtFrom);
                Assert.True(args.FetchReceivedTransfers || args.FetchSentTransfers);
            }
            catch
            {
                throw EnumException.Create(
                    EGeneralWalletLocalApiErrorCodes20151004.WrongArgs);
            }
        }
        /**/
        public class FetchTransferFromDataCursor20151004Args
        {
            public int Offset;
            public int Count;
        }

        public static void CheckFetchTransferFromDataCursor20151004Args(
            FetchTransferFromDataCursor20151004Args args,
            int currentTransferListCount
            )
        {
            try
            {
                Assert.NotNull(args);
                Assert.InRange(
                    args.Offset,
                    0,
                    currentTransferListCount
                );
                Assert.True(args.Count >= 0);
            }
            catch
            {
                throw EnumException.Create(
                    EGeneralWalletLocalApiErrorCodes20151004.WrongArgs);
            }
        }
    }
    /**/
    public enum ESentRequestStatus20151004
    {
        NotFound,
        PreparedToSend,
        SendFault,
        Sent
    }
    public enum ESentRequestFaultErrCodes20151004
    {
        NoErrors,
        WalletToNotExist,
        ServerException,
        //Exception message in string value
        UnknownError
    }
    /*
    public enum EProcessSimpleTransferErrCodes
    {
        NoErrors,
        WalletToNotExist,
        NotEnoughFunds,
        CommentKeyIsNotRegistered,
        WrongCommentKey,
        ExpiredCommentKey,
        CommentSizeTooBig,
        WrongTransferAmount,
        TransferAmountLessThanWalletToRequires,
        AnonymousKeyWithNotAnonymousTransfer,
        NotAnonymousKeyWithAnonymousTransfer,
        TransferGuidAlreadyRegistered,
        ExpectedFeeMoreThanRequestMaxFee
    }
    */
    /*
    public enum EWalletGeneralErrCodes
    {
        NoErrors = 100,
        WrongRequest,
        WalletGuidForbidden,
        OtherWalletGuidForbidden,
        WrongServerWalletGuidRange, 
        OtherWalletGuidNotExist,
        AlreadyRegisteredByOtherWallet,
        WalletNotRegistered,
        OtherWalletNotRegistered
    }
    */
    public class GetSentTransferRequestStatus20151004Response
    {
        public ESentRequestStatus20151004 Status;
        public List<Guid> RelatedTransferGuidList = new List<Guid>();
        public ESentRequestFaultErrCodes20151004 ErrCode 
            = ESentRequestFaultErrCodes20151004.NoErrors;
        public string ErrMessage = "";
        public EProcessSimpleTransferErrCodes ServerErrCode
            = EProcessSimpleTransferErrCodes.NoErrors;
        public EWalletGeneralErrCodes GeneralServerErrCode
            = EWalletGeneralErrCodes.NoErrors;
    }
    /**/
    public class TransferInfo20151004
    {
        public bool OutcomeTransfer;
        public Guid TransferGuid;
        public string SentTimeString;
        public Guid RequestGuid;
        public long Amount;
        public bool AnonymousTransfer;
        public Guid WalletFrom;
        public Guid WalletTo;
        public long Fee;
        public int RelativeTransferNum;
        public byte[] CommentBytes;
        public bool AuthenticatedOtherWalletCert;
        public bool AuthenticatedCommentKey;
        public bool AuthenticatedTransferDetails;
    }
    /**/
    public enum ETransferDataCursorStatus20151004
    {
        Fetching,
        Complete,
        WalletDisconnected,
        UnknownError
    }
    public class GetTransferDataCursorStatusResponse20151004
    {
        public ETransferDataCursorStatus20151004 Status;
        public Guid BaseWalletGuid;
        public int TotalTransferCount;
    }
    /**/
    public partial interface IWalletLocalJsonRpcApi
    {
        /* throw EGeneralWalletLocalApiErrorCodes20151004 */
        Task<long> GetBalance20151004(Guid walletGuid);

        /* throw EGeneralWalletLocalApiErrorCodes20151004 */
        Task<bool> IsWalletConnected20151004(Guid walletGuid);

        /* throw EGeneralWalletLocalApiErrorCodes20151004 */
        Task<bool> IsWalletRpcAllowed20151004(Guid walletGuid);

        /* throw EGeneralWalletLocalApiErrorCodes20151004 */
        Task<bool> HmacSignatureRpcRequired20151004();

        /* 
            requestGuid - if Guid.Empty new will be generated
            throw EGeneralWalletLocalApiErrorCodes20151004 
            return requestGuid, or new if was empty
        */
        Task<Guid> SendTransfer20151004(
            Guid requestGuid,
            Guid walletFromGuid,
            Guid walletToGuid,
            long amount,
            string sentTimeString,
            bool anonymousTransfer,
            byte[] commentBytes,
            long maxFee = 0, // 0 doesnt'check
            byte[] hmacAuthCode = null
        );

        /* 
            Estimate fee 
            throw EGeneralWalletLocalApiErrorCodes20151004
        */
        Task<long> EstimateFee20151004(
            Guid walletFrom,
            Guid walletTo,
            long amount,
            int commentBytesLength,
            bool anonymousTransfer
        );

        /* 
            Check wallet to exist 
            throw EGeneralWalletLocalApiErrorCodes20151004
        */
        Task<bool> IsWalletRegistered20151004(
            Guid baseWalletGuid,
            Guid walletGuid,
            bool recheck = false
        );

        /* 
            Check sendtransfer request status
            throw EGeneralWalletLocalApiErrorCodes20151004
        */
        Task<GetSentTransferRequestStatus20151004Response> GetSentTransferRequestStatus20151004(
            Guid baseWalletGuid,
            Guid requestGuid,
            bool recheck = false
        );

        /* 
            Get sent transfer info
            throw EGeneralWalletLocalApiErrorCodes20151004
            return null if not found
        */
        Task<TransferInfo20151004> GetSentTransferInfo20151004(
            Guid baseWalletGuid,
            Guid trasferGuid,
            bool recheck = false
        );

        /* 
            Get received transfer info
            throw EGeneralWalletLocalApiErrorCodes20151004
            return null if not found
        */
        Task<TransferInfo20151004> GetReceivedTransferInfo20151004(
            Guid baseWalletGuid,
            Guid transferGuid,
            bool recheck = false
        );

        /* 
            Create data cursor
            throw EGeneralWalletLocalApiErrorCodes20151004
        */
        Task<int> CreateTransferDataCursor20151004(
            Guid baseWalletGuid,
            string dateTimeFrom,
            string dateTimeTo,
            bool fetchSentTransfers = true,
            bool fetchReceivedTransfers = true,
            bool stayOnline = true
        );
        
        /* 
            Get data cursor status 
            throw EGeneralWalletLocalApiErrorCodes20151004
        */
        Task<GetTransferDataCursorStatusResponse20151004> 
            GetTransferDataCursorStatus20151004(
                int dataCursorNum
            );

        /* 
            Fetch transfer list from cursor 
            throw EGeneralWalletLocalApiErrorCodes20151004
        */
        Task<List<TransferInfo20151004>> FetchTransferFromDataCursor20151004(
            int dataCursorNum,
            int offset,
            int count
        );

        /*
            Close data cursor 
            throw EGeneralWalletLocalApiErrorCodes20151004
        */
        Task CloseTransferDataCursor20151004(
            int dataCursorNum
        );
    }
}
