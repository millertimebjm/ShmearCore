var seats = ['', '', '', ''];
var buttons;
var shmearHub;
var gameId = 0;
var gameStarted = false;
var highestWager = 0;
var i = 0;
var handCardIds = ['', '', '', '', '', ''];

$().ready(function () {
    // name = $("#name").val();
    name = "asdf";
    buttons = [$('#seat1'), $('#seat2'), $('#seat3'), $('#seat4')];
    wagers = [$('#wager2link'), $('#wager3link'), $('#wager4link'), $('#wager5link')];

    console.log("Connection Created");
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/shmearHub")
        .build();

    function getParameterByName(name) {
        name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
        var regex = new RegExp("[\\?&]" + name + "=([^&#]*)"),
            results = regex.exec(location.search);
        return results === null ? "" : decodeURIComponent(results[1].replace(/\+/g, " "));
    }

    connection.on("Logout", (message) => {
        console.log("Logout called from Server");
        alert(message);
        window.location.replace('http://' + document.location.host + '/');
    });

    console.log("Connection Started");
    var promise = connection.start();
    promise.then(function () {
        var name = getParameterByName('name');
        console.log("SetPlayerName being called from Client:  " + name);
        connection.invoke("SetPlayerName", name);
    });

    connection.on("ReceiveSeatStatuses", (openGameId, seatArray) => {
        console.log("ReceiveSeatStatuses called from Server");
        //console.log('Received seat statuses, connection ID=' + $.connection.hub.id);
        seats = seatArray;
        gameId = openGameId;
        if (!gameStarted) {
            $('#seatDiv').show();
            $('#cardDiv').hide();
        } else {
            $('#cardDiv').show();
            $('#seatDiv').hide();
        }
        seatButtonUpdate();
        readyButtonUpdate();
    });

    connection.on("UpdatePlayerReadyStatus", (ready) => {
        console.log("UpdatePlayerReadyStatus being called from Server");
        $('#ready').removeClass('btn-default');
        $('#ready').removeClass('btn-success');
        if (ready) {
            $('#ready').addClass('btn-success');
            $('#ready').text('Ready');
            $('#title').text('Wait for remaining players');
        } else {
            $('#ready').addClass('btn-default');
            $('#ready').text('Not Ready');
            $('#title').text('Toggle the Ready button');
        }
    });

    connection.on("CardUpdate", (playerIndex, cards, cardCountBySeat) => {
        console.log("CardUpdate called from Server");
        for (i = 0; i < 6; i++) {
            $('#player'.concat(playerIndex + 1).concat('card').concat(i + 1).concat(' a')).prop("onclick", null);
            $('#player'.concat(playerIndex + 1).concat('card').concat(i + 1)).html('');
            handCardIds[i] = 0;
        }

        for (i = 0; i < cards.length; i++) {
            handCardIds[i] = cards[i][0];
            var card = cards[i];
            var cardId = card[0];
            var cardString = card[1];
            var playerCardId = 'player'.concat(playerIndex + 1).concat('card').concat(i + 1);
            var playerCardAnchorId = playerCardId.concat('anchor');
            $('#'.concat(playerCardId)).html('<a id="' + playerCardAnchorId + '" href="#">'.concat("<img src='/images/Cards/" + cardString + ".png' width='100' height='144'>").concat('</a>&nbsp;'));
            console.log(playerCardAnchorId.concat(' ').concat(cardId));

            $('#'.concat(playerCardAnchorId)).click(function () {
                console.log($(this).attr('id').charAt(11));
                console.log(handCardIds[$(this).attr('id').charAt(11)]);
                console.log("PlayCard is being called on from the Client");
                var cardId = parseInt(handCardIds[$(this).attr('id').charAt(11) - 1]);
                connection.invoke("PlayCard", gameId, cardId);
            });
        };

        for (i = 0; i < 4; i++) {
            if (!(i === playerIndex)) {
                $('#player'.concat(i + 1).concat('card').concat(1)).text(cardCountBySeat[i]);
            }
        }
        $('#seatDiv').hide();
        $('#cardDiv').show();
    })

    connection.on("HideWager", () => {
        console.log("HideWager being called from Server");
        $('#wager0').hide();
        for (i = 0; i < 4; i++) {
            wagers[i].hide();
        }
    });

    connection.on("PlayerTurnUpdate", (playerSeatTurn) => {
        console.log("PlayerTurnUpdate being called from Server");
        for (i = 1; i < 5; i++) {
            if (playerSeatTurn === i) {
                $('#player'.concat(i).concat('arrow')).show();
            } else {
                $('#player'.concat(i).concat('arrow')).hide();
            }
        }
    });

    connection.on("WagerUpdate", (highestWagerInput) => {
        console.log("WagerUpdate being called from Server");
        $('#wager0').show();
        highestWager = highestWagerInput;
        for (i = 0; i < 4; i++) {
            if ((i + 2) > highestWager) {
                wagers[i].show();
            } else {
                wagers[i].hide();
            }
        }
    });

    connection.on("SendMessage", (message) => {
        console.log("SendMessage being called from Server");
        $('#messages').html('<p>'.concat(message).concat('</p>').concat($('#messages').html()));
    });

    $('#wager0link').click(function () {
        var wager = parseInt($('#wager0').text());
        console.log("SetWager being called from Client.  GameId: " + gameId + " | WagerId: " + wager);
        connection.invoke("SetWager", gameId, wager);
    });

    $('#ready').click(function () {
        console.log("TogglePlayerReadyStatus being called from Client");
        connection.invoke("TogglePlayerReadyStatus", gameId);
    });

    function seatButtonUpdate() {
        for (var i = 0; i < 4; i++) {
            buttons[i].removeClass('btn-primary');
            buttons[i].removeClass('btn-default');
            buttons[i].removeClass('btn-success');
            buttons[i].text(seats[i]);
            if (seats[i] === '') {
                buttons[i].addClass('btn-primary');
                buttons[i].text(i + 1);
            }
            if (seats[i] === name) {
                buttons[i].addClass('btn-success');
            }
            if (!(seats[i] === name) && !(seats[i] === '')) {
                buttons[i].addClass('btn-default');
            }
        }

        for (var i = 0; i < 4; i++) {
            if (!(seats[i] === '')) {
                break;
            }
            if (i === 3) {
                $('ready').show();
                return;
            }
        };

        for (var i = 0; i < 4; i++) {
            if (!(seats[i] === '')) {
                $('#player'.concat(i + 1)).text(seats[i]);
            }
        };

        $('ready').hide();
    };

    function readyButtonUpdate() {
        $('#title').html('Pick a seat');
        $('#ready').hide();
        for (var j = 0; j < 4; j++) {
            if (buttons[j].text() === '@Model.Name') {
                $('#title').html('Toggle the Ready button');
                $('#ready').show();
            }
        };
    };

    $('#leave').click(function () {
        console.log("Calling the LeaveSeat function from Client");
        connection.invoke("LeaveSeat", gameId);
        $('#ready').hide();
        $('#cardDiv').hide();
        $('#seatDiv').show();
    });

    for (var i = 0; i < 4; i++) {
        buttons[i].click(function () {
            var buttonId = parseInt($(this).val());
            console.log("SetSeatStatus being called from Client.  GameId: " + gameId + " | buttonId: " + buttonId);
            var promise = connection.invoke("SetSeatStatus", gameId, buttonId);
            promise.then(function () {
                readyButtonUpdate();
            });
        });
    };

    for (i = 0; i < 4; i++) {
        wagers[i].click(function () {
            var wager = parseInt($(this).text());
            console.log("SetWager being called from Client.  GameId: " + gameId + " | WagerId: " + wager);
            connection.invoke("SetWager", gameId, wager);
        });
    }

    $('#sendMessageButton').click(function () {
        var message = parseInt($('#playerMessage').val());
        console.log("SendChat being called from Client");
        connection.invoke("SendChat", gameId, message);
        $('#playerMessage').val('');
    });
});