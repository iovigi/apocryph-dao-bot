﻿using Apocryph.Dao.Bot.Core.Data.Message;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Parity;
using Nethereum.RPC.Eth.DTOs;
using Newtonsoft.Json.Linq;
using Perper.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace Apocryph.Dao.Bot.Core.Streams
{
    public class TransferEvents
    {
        private readonly IContext context;
        private readonly Web3Parity web3;

        public TransferEvents(IContext context, Web3Parity web3)
        {
            this.context = context;
            this.web3 = web3;
        }

        public async IAsyncEnumerable<TransferEventMessage> RunAsync()
        {
            var transferEventHandler = web3.Eth.GetEvent<TransferEventDTO>((string)config["contracts"]["bondingCurveToken"]["address"]);
            var transferFilterInput = transferEventHandler.CreateFilterInput(new BlockParameter(ulong.Parse((string)config["startBlock"])), BlockParameter.CreateLatest());
            var allChanges = await transferEventHandler.GetAllChangesAsync(transferFilterInput);
            foreach (var change in allChanges)
            {
                yield return ConvertToMessage(change.Event);
            }

            var transferFilterInputId = await transferEventHandler.CreateFilterAsync(transferFilterInput);
            while (true)
            {
                var changes = await transferEventHandler.GetFilterChangesAsync(transferFilterInputId);
                foreach (var change in changes)
                {
                    yield return ConvertToMessage(change.Event);
                }

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        private TransferEventMessage ConvertToMessage(TransferEventDTO eventDTO)
        {
            return new TransferEventMessage
            {
                Sender = eventDTO.From,
                Receiver = eventDTO.To,
                Amount = eventDTO.Value
            };
        }
    }

    [Event("Transfer")]
    public class TransferEventDTO : IEventDTO
    {
        [Parameter("address", "from", 1, true)]
        public string From { get; set; }

        [Parameter("address", "to", 2, true)]
        public string To { get; set; }

        [Parameter("uint256", "value", 3, false)]
        public BigInteger Value { get; set; }
    }

}