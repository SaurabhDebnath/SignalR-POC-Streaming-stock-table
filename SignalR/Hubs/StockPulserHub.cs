using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using SignalR.Models;
using System.Collections.Generic;

namespace SignalR.Hubs
{
    [HubName("pulser")]
    public class StockPulserHub : Hub
    {
        private readonly StockPulser _stockPulser;

        public StockPulserHub() :
            this(StockPulser.Instance)
        {

        }

        public StockPulserHub(StockPulser stockPulser)
        {
            _stockPulser = stockPulser;
        }

        public IEnumerable<Stock> GetAllStocks()
        {
            return _stockPulser.GetAllStocks();
        }

        public string GetMarketState()
        {
            return _stockPulser.PulsingState.ToString();
        }

        public void StartPulsing()
        {
            _stockPulser.StartPulsing();
        }

        public void StopPulsing()
        {
            _stockPulser.StopPulsing();
        }

        public void Reset()
        {
            _stockPulser.Reset();
        }
    }
}