using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.SignalR.Hubs;
using SignalR.Models;
using Microsoft.AspNet.SignalR;

namespace SignalR.Hubs
{
    public sealed class StockPulser
    {
        // Singleton instance
        private readonly static Lazy<StockPulser> _instance = new Lazy<StockPulser>(
            () => new StockPulser(GlobalHost.ConnectionManager.GetHubContext<StockPulserHub>().Clients));

        private readonly object _pulsingStateLock = new object();
        private readonly object _updateStockPricesLock = new object();

        private readonly ConcurrentDictionary<string, Stock> _stocks = new ConcurrentDictionary<string, Stock>();

        private const double _rangePercent = 0.002;

        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(500);
        private readonly Random _updateOrNotRandom = new Random();

        private Timer _timer;
        private volatile bool _updatingStockPrices;
        private volatile PulsingState _pulsingState;

        private StockPulser(IHubConnectionContext<dynamic> clients)
        {
            Clients = clients;
            LoadAllStocks();
        }

        public static StockPulser Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        private IHubConnectionContext<dynamic> Clients { get; set; }

        public PulsingState PulsingState
        {
            get { return _pulsingState; }
            private set { _pulsingState = value; }
        }

        public IEnumerable<Stock> GetAllStocks()
        {
            return _stocks.Values;
        }

        public void StartPulsing()
        {
            lock (_pulsingStateLock)
            {
                if (PulsingState != PulsingState.Open)
                {
                    _timer = new Timer(UpdateStockPrices, null, _updateInterval, _updateInterval);

                    PulsingState = PulsingState.Open;

                    BroadcastPulsingStateChange(PulsingState.Open);
                }
            }
        }

        public void StopPulsing()
        {
            lock (_pulsingStateLock)
            {
                if (PulsingState == PulsingState.Open)
                {
                    if (_timer != null)
                    {
                        _timer.Dispose();
                    }

                    PulsingState = PulsingState.Closed;

                    BroadcastPulsingStateChange(PulsingState.Closed);
                }
            }
        }

        public void Reset()
        {
            lock (_pulsingStateLock)
            {
                if (PulsingState != PulsingState.Closed)
                {
                    throw new InvalidOperationException("Market must be closed before it can be reset.");
                }

                LoadAllStocks();
                BroadcastMarketReset();
            }
        }

        private void LoadAllStocks()
        {
            _stocks.Clear();

            var stocks = new List<Stock>
            {
                new Stock { Symbol = "AMZN", Price = 41.68m },
                new Stock { Symbol = "AAPL", Price = 92.08m },
                new Stock { Symbol = "GOOG", Price = 543.01m },
                new Stock { Symbol = "IBM", Price = 343.01m }
            };

            stocks.ForEach(stock => _stocks.TryAdd(stock.Symbol, stock));
        }

        private void UpdateStockPrices(object state)
        {
            // This function must be re-entrant as it's running as a timer interval handler
            lock (_updateStockPricesLock)
            {
                if (!_updatingStockPrices)
                {
                    _updatingStockPrices = true;

                    foreach (var stock in _stocks.Values)
                    {
                        if (TryUpdateStockPrice(stock))
                        {
                            BroadcastStockPrice(stock);
                        }
                    }

                    _updatingStockPrices = false;
                }
            }
        }

        private bool TryUpdateStockPrice(Stock stock)
        {
            // Randomly choose whether to udpate this stock or not
            var r = _updateOrNotRandom.NextDouble();
            if (r > 0.1)
            {
                return false;
            }

            // Update the stock price by a random factor of the range percent
            var random = new Random((int)Math.Floor(stock.Price));
            var percentChange = random.NextDouble() * _rangePercent;
            var pos = random.NextDouble() > 0.51;
            var change = Math.Round(stock.Price * (decimal)percentChange, 2);
            change = pos ? change : -change;

            stock.Price += change;
            return true;
        }

        private void BroadcastPulsingStateChange(PulsingState marketState)
        {
            switch (marketState)
            {
                case PulsingState.Open:
                    Clients.All.startPulsing();
                    break;
                case PulsingState.Closed:
                    Clients.All.stopPulsing();
                    break;
                default:
                    break;
            }
        }

        private void BroadcastMarketReset()
        {
            Clients.All.pulseReset();
        }

        private void BroadcastStockPrice(Stock stock)
        {
            Clients.All.updateStockPrice(stock);
        }
    }

    public enum PulsingState
    {
        Closed,
        Open
    }
}