// var seats = ['', '', '', ''];
// var buttons;
// var shmearHub;
var gameId = 0;

// var gameStarted = false;
// var highestWager = 0;
// var i = 0;
// var handCardIds = ['', '', '', '', '', ''];
var username = '';

$().ready(function () {
    var urlParams = new URLSearchParams(window.location.search);
    username = urlParams.get('name');
    if (!username || username === '') {
        window.location = "index.html";
    }

    // buttons = [$('#seat1'), $('#seat2'), $('#seat3'), $('#seat4')];
    // wagers = [$('#wager2link'), $('#wager3link'), $('#wager4link'), $('#wager5link')];
    console.log("Connection Created");
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/shmearHub")
        // .WithKeepAliveInterval(30)
        // .WithConnectionTimeout(600)
        .build();
    connection.on("Logout", (message) => {
        console.log("Logout called from Server");
        alert(message);
        window.location = "index.html";
    });
    console.log("Connection Started");
    var promise = connection.start({ withCredentials: false });
    promise.then(function () {
        console.log("SetPlayerName being called from Client:  " + username);
        connection.invoke("SetPlayerName", username);
    });
    connection.on("ReceiveSeatStatuses", (openGameId, seatArray) => {
        console.log("ReceiveSeatStatuses called from Server");
        var seats = $('.seat');
        $.each(seats, function (index, value) {
            var seatUsername = seatArray[$(value).data('seatnumber') - 1];
            if (seatUsername === "") {
                seatUsername = "Open";
            }
            $(value).find(".seatUsername").text(seatUsername);
        });
        gameId = openGameId;
        // seatButtonUpdate();
    });
    $(".card").on("click", function (e) {
        console.log(e.target);
        var cardId = $(e.target).closest(".card").data("cardid");
        var promise = connection.invoke("PlayCard", gameId, cardId);
        promise.then(function (output) {
            if (output) {
                console.log("PlayCard was successful with cardId: " + cardId);
                var playedCard = $('.card [data-cardid]="' + cardId + '"');
                $(playedCard).attr("src", "images/Cards/Empty.png");
                $(playedCard).data("cardid", 0);
            }
        });
    });
    $(".seat").on("click", function (e)
    {
        console.log(e.target);
        var seatNumber = $(e.target).closest(".seat").data("seatnumber");
        console.log("SetSeatStatus being called from Client.  GameId: " + gameId + " | seatNumber: " + seatNumber);
        var promise = connection.invoke("SetSeatStatus", gameId, seatNumber);
    })
    connection.on("CardUpdate", (cards) => {
        console.log("CardUpdate called from Server");
        var cardDivs = $(".card");
        for (let i = 0; i < cards.length; i++) {
            $(cardDivs[i]).attr("src", "/images/Cards/" + cards[i][1] + ".png");
            $(cardDivs[i]).data("cardid", cards[i][0]);
        }

        $('#seatDiv').hide();
        $('#gameDiv').show();
    });
    connection.on("RequestWager", (currentWager) => {
        console.log("RequestWager called from Server.  MaxWager: " + currentWager);
        var wagers = $(".wager");
        $(".wager").hide();
        $.each(wagers, function (index, value) {
            var wagerValue = $(value).data("wager");
            if (wagerValue === 0 || wagerValue > currentWager) {
                $(value).show();
            }
        });
        $("#wagerDiv").show();
    });
    $('.wager').click(function (e) {
        var wager = $(e.target).closest(".wager").data("wager");
        console.log("SetWager being called from Client.  GameId: " + gameId + " | WagerId: " + wager);
        connection.invoke("SetWager", gameId, wager);
        $(".wager").hide();
    });
    connection.on("PlayerTurnUpdate", (playerSeatTurn) => {
        console.log("PlayerTurnUpdate being called from Server.  PlayerSeatTurn: " + playerSeatTurn);
        // for (i = 1; i < 5; i++) {
        //     if (playerSeatTurn === i) {
        //         $('#player'.concat(i).concat('arrow')).show();
        //     } else {
        //         $('#player'.concat(i).concat('arrow')).hide();
        //     }
        // }
    });
    
    // connection.on("CardUpdate", (playerIndex, cards, cardCountBySeat) => {
    //     console.log("CardUpdate called from Server");
    //     for (i = 0; i < 6; i++) {
    //         //$('#player'.concat(playerIndex + 1).concat('card').concat(i + 1).concat(' a')).prop("onclick", null);
    //         //$('#player'.concat(playerIndex + 1).concat('card').concat(i + 1)).html('');
    //         $('#card'.concat(i + 1).concat(' a')).prop("onclick", null);
    //         $('#card'.concat(i + 1)).html('');
    //         handCardIds[i] = 0;
    //     }
    //     for (i = 0; i < cards.length; i++) {
    //         handCardIds[i] = cards[i][0];
    //         var card = cards[i];
    //         var cardId = card[0];
    //         var cardString = card[1];
    //         //var playerCardId = 'player'.concat(playerIndex + 1).concat('card').concat(i + 1);
    //         var playerCardId = 'card'.concat(i + 1);
    //         var playerCardAnchorId = playerCardId.concat('anchor');
    //         $('#'.concat(playerCardId)).html('<a id="' + playerCardAnchorId + '" href="#">'.concat("<img src='/images/Cards/" + cardString + ".png' style='max-width: 100%; max-height: 100%'>").concat('</a>&nbsp;'));
    //         //width='100' height='144'
    //         console.log(playerCardAnchorId.concat(' ').concat(cardId));
    //         $('#'.concat(playerCardAnchorId)).click(function () {
    //             console.log($(this).attr('id').charAt(11));
    //             //console.log(handCardIds[$(this).attr('id').charAt(11)]);
    //             //player1card1
    //             //card1
    //             console.log(handCardIds[$(this).attr('id').charAt(4)]);
    //             console.log("PlayCard is being called on from the Client");
    //             //var cardId = parseInt(handCardIds[$(this).attr('id').charAt(11) - 1]);
    //             var cardId = parseInt(handCardIds[$(this).attr('id').charAt(4) - 1]);
    //             connection.invoke("PlayCard", gameId, cardId);
    //         });
    //     };
    //     for (i = 0; i < 4; i++) {
    //         if (!(i === playerIndex)) {
    //             $('#player'.concat(i + 1).concat('card').concat(1)).text(cardCountBySeat[i]);
    //         }
    //     }
    //     $('#seatDiv').hide();
    //     $('#cardDiv').show();
    // })
    // connection.on("HideWager", () => {
    //     console.log("HideWager being called from Server");
    //     $('#wager0').hide();
    //     for (i = 0; i < 4; i++) {
    //         wagers[i].hide();
    //     }
    // });
    // connection.on("PlayerTurnUpdate", (playerSeatTurn) => {
    //     console.log("PlayerTurnUpdate being called from Server");
    //     for (i = 1; i < 5; i++) {
    //         if (playerSeatTurn === i) {
    //             $('#player'.concat(i).concat('arrow')).show();
    //         } else {
    //             $('#player'.concat(i).concat('arrow')).hide();
    //         }
    //     }
    // });
    // connection.on("WagerUpdate", (highestWagerInput) => {
    //     console.log("WagerUpdate being called from Server");
    //     $('#wager0').show();
    //     highestWager = highestWagerInput;
    //     for (i = 0; i < 4; i++) {
    //         if ((i + 2) > highestWager) {
    //             wagers[i].show();
    //         } else {
    //             wagers[i].hide();
    //         }
    //     }
    // });
    // connection.on("SendMessage", (message) => {
    //     console.log("SendMessage being called from Server");
    //     $('#messages').html('<p>'.concat(message).concat('</p>').concat($('#messages').html()));
    // });
    // $('#wager0link').click(function () {
    //     var wager = parseInt($('#wager0').text());
    //     console.log("SetWager being called from Client.  GameId: " + gameId + " | WagerId: " + wager);
    //     connection.invoke("SetWager", gameId, wager);
    // });
    // $('#ready').click(function () {
    //     console.log("TogglePlayerReadyStatus being called from Client");
    //     connection.invoke("TogglePlayerReadyStatus", gameId);
    // });

    // $('#leave').click(function () {
    //     console.log("Calling the LeaveSeat function from Client");
    //     connection.invoke("LeaveSeat", gameId);
    //     $('#ready').hide();
    //     $('#cardDiv').hide();
    //     $('#seatDiv').show();
    // });
    // for (i = 0; i < 4; i++) {
    //     wagers[i].click(function () {
    //         var wager = parseInt($(this).text());
    //         console.log("SetWager being called from Client.  GameId: " + gameId + " | WagerId: " + wager);
    //         connection.invoke("SetWager", gameId, wager);
    //     });
    // }
    // $('#sendMessageButton').click(sendMessageButtonFunction);
    // $("#playerMessage").bind("keypress", checkPlayerMessageEnterKey);

    // function checkPlayerMessageEnterKey(e) {
    //     if (e.keyCode === 13) {
    //         e.preventDefault(); // Ensure it is only this code that runs
    //         sendMessageButtonFunction();
    //     }
    // }

    // function sendMessageButtonFunction() {
    //     var message = $('#playerMessage').val();
    //     console.log("SendChat being called from Client");
    //     connection.invoke("SendChat", gameId, message);
    //     $('#playerMessage').val('');
    // }
});