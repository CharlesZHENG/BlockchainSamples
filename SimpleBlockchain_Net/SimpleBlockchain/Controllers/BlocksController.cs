using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace SimpleBlockchain.Controllers
{
    [Route("api/blocks")]
    public class BlocksController : Controller
    {
        private readonly IMemoryCache _cache;
        private const string Key = "blockchain";
        private static int _difficulty = 16;

        private readonly object _lockObject = new object();

        public BlocksController(IMemoryCache cache)
        {
            _cache = cache;
            if (!_cache.TryGetValue(Key, out List<Block> _))
            {
                _cache.Set(Key, new List<Block> { GenerateGenesisBlock() });
            }
        }

        [HttpGet]
        public IActionResult List()
        {
            var blocks = _cache.Get<List<Block>>(Key);
            return Ok(blocks);
        }

        [HttpPost]
        public IActionResult Create()
        {
            var blocks = _cache.Get<List<Block>>(Key);
            var lastBlock = blocks.Last();
            var bpm = GetRandomNum(1, 50);
            var block = GenerateBlock(lastBlock, bpm);

            blocks = _cache.Get<List<Block>>(Key);
            lastBlock = blocks.Last();
            if (IsBlockValid(block, lastBlock))
            {
                var newBlocks = new List<Block>();
                newBlocks.AddRange(blocks);
                newBlocks.Add(block);

                blocks = _cache.Get<List<Block>>(Key);
                if (newBlocks.Count > blocks.Count)
                {
                    lock (_lockObject)
                    {
                        _cache.Set(Key, newBlocks);
                    }
                    return Created("List", block);
                }
            }
            return Ok("Opps, someone is faster than you!");
        }

        private static Block GenerateGenesisBlock()
        {
            var genesisBlock = new Block
            {
                Index = 0,
                TimeStamp = CalculateTimeStamp(),
                BPM = 0,
                Hash = string.Empty,
                PrevHash = string.Empty,
                Difficulty = 0
            };

            return genesisBlock;
        }

        private static Block GenerateBlock(Block prevBlock, int bpm)
        {
            var block = new Block
            {
                Index = prevBlock.Index + 1,
                TimeStamp = CalculateTimeStamp(),
                BPM = bpm,
                PrevHash = prevBlock.Hash,
                Difficulty = _difficulty
            };

            CalculateNonce(block);
            return block;
        }

        private static void CalculateNonce(Block block)
        {
            for (var i = 0; ; i++)
            {
                block.Nonce = i.ToString("X2");
                var hash = CalculateHash(block);
                if (IsHashValid(hash))
                {
                    block.Hash = hash;
                    break;
                }
                Console.WriteLine($"{hash} is invalid, continue calculating...");
            }
        }

        private static string CalculateHash(Block block)
        {
            var record = $"{block.Index}{block.TimeStamp}{block.BPM}{block.PrevHash}{block.Nonce}";

            var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(record));

            var builder = new StringBuilder();
            foreach (var @byte in bytes)
            {
                builder.Append(@byte.ToString("X2"));
            }

            return builder.ToString();
        }

        private static long CalculateTimeStamp()
        {
            var start = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var now = DateTime.Now;
            return (long)Math.Round((now - start).TotalMilliseconds, MidpointRounding.AwayFromZero);
        }

        private static bool IsBlockValid(Block block, Block prevBlock)
        {
            if (block.Index != prevBlock.Index + 1)
            {
                return false;
            }

            if (block.PrevHash != prevBlock.Hash)
            {
                return false;
            }

            return CalculateHash(block) == block.Hash;
        }

        private static bool IsHashValid(string hash)
        {
            var bytes = Enumerable.Range(0, hash.Length)
                .Where(n => n % 2 == 0)
                .Select(n => Convert.ToByte(hash.Substring(n, 2), 16))
                .ToArray();

            var bits = new BitArray(bytes);

            for (var i = 0; i < _difficulty; i++)
            {
                if (bits[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetRandomNum(int min, int max)
        {
            using (var provider = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[4];
                provider.GetBytes(bytes);
                var scale = BitConverter.ToUInt32(bytes, 0);
                return (int)(min + (max - min) * (scale / (uint.MaxValue + 1.0)));
            }
        }
    }
}

