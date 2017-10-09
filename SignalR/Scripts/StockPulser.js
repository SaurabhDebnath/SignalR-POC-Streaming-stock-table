if (!String.prototype.supplant) {
    String.prototype.supplant = function (o) {
        return this.replace(/{([^{}]*)}/g,
            function (a, b) {
                var r = o[b];
                return typeof r === 'string' || typeof r === 'number' ? r : a;
            }
        );
    };
}

jQuery.fn.flash = function (color, duration) {
    var current = this.css('backgroundColor');
    this.animate({ backgroundColor: 'rgb(' + color + ')' }, duration / 2).animate({ backgroundColor: current }, duration / 2);
};

$(function () {

    var hubProxy = $.connection.pulser,
        up = '▲',
        down = '▼',
        $stockTable = $('#stockTable'),
        $stockTableBody = $stockTable.find('tbody'),
        rowTemplate = '<tr data-symbol="{Symbol}"><td>{Symbol}</td><td>{Price}</td><td>{DayOpen}</td><td>{DayHigh}</td><td>{DayLow}</td><td><span class="dir {DirectionClass}">{Direction}</span> {Change}</td><td>{LastChange}</td><td>{PercentChange}</td></tr>',
        $marquee = $('#marquee'),
        $marqueeUl = $marquee.find('ul'),
        liTemplate = '<li data-symbol="{Symbol}"><span class="symbol">{Symbol}</span> <span class="price">{Price}</span> <span class="change"><span class="dir {DirectionClass}">{Direction}</span> {Change} ({PercentChange})</span></li>';

    function formatStock(stock) {
        return $.extend(stock, {
            Price: stock.Price.toFixed(2),
            PercentChange: (stock.PercentChange * 100).toFixed(2) + '%',
            Direction: stock.Change === 0 ? '' : stock.Change >= 0 ? up : down,
            DirectionClass: stock.Change === 0 ? 'even' : stock.Change >= 0 ? 'up' : 'down'
        });
    }

    function marquee() {
        var w = $marqueeUl.width();
        $marqueeUl.css({ marginLeft: w });
        $marqueeUl.animate({ marginLeft: -w }, 15000, 'linear', marquee);
    }

    function stopMarquee() {
        $marqueeUl.stop();
    }

    function init() {
        return hubProxy.server.getAllStocks().done(function (stocks) {
            $stockTableBody.empty();
            $marqueeUl.empty();
            $.each(stocks, function () {
                var stock = formatStock(this);
                $stockTableBody.append(rowTemplate.supplant(stock));
                $marqueeUl.append(liTemplate.supplant(stock));
            });
        });
    }

    $.extend(hubProxy.client, {
        updateStockPrice: function (stock) {

            var displayStock = formatStock(stock),
                $row = $(rowTemplate.supplant(displayStock)),
                $li = $(liTemplate.supplant(displayStock)),
                bg = stock.LastChange < 0
                        ? '255,148,148' // red
                        : '154,240,117'; // green

            $stockTableBody.find('tr[data-symbol=' + stock.Symbol + ']')
                .replaceWith($row);
            $marqueeUl.find('li[data-symbol=' + stock.Symbol + ']')
                .replaceWith($li);

            $row.flash(bg, 1000);
            $li.flash(bg, 1000);
        },

        startPulsing: function () {
            $("#startPulsing").prop("disabled", true);
            $("#stopPulsing").prop("disabled", false);
            $("#reset").prop("disabled", true);
            marquee();
        },

        stopPulsing: function () {
            $("#startPulsing").prop("disabled", false);
            $("#stopPulsing").prop("disabled", true);
            $("#reset").prop("disabled", false);
            stopMarquee();
        },

        pulseReset: function () {
            return init();
        }
    });

    $.connection.hub.start()
        .then(init)
        .then(function () {
            return hubProxy.server.getMarketState();
        })
        .done(function (state) {
            if (state === 'Open') {
                hubProxy.client.startPulsing();
            } else {
                hubProxy.client.stopPulsing();
            }

            // Wire up the buttons
            $("#startPulsing").click(function () {
                hubProxy.server.startPulsing();
            });

            $("#stopPulsing").click(function () {
                hubProxy.server.stopPulsing();
            });

            $("#reset").click(function () {
                hubProxy.server.reset();
            });
        });
});