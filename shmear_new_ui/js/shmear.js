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
        .withUrl("http://miller.silverminenordic.com:8443/shmearHub")
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

    document.addEventListener("dragstart", function (event) {
        console.log("dragstart - " + event.target.id);
        event.dataTransfer.setData("text/plain", event.target.id);
    });
    document.addEventListener("dragover", function (event) {
        event.preventDefault();
    });
    document.addEventListener("drop", function (event) {
        console.log("drop - + " + event.target.id);
        event.preventDefault();
        var data = event.dataTransfer.getData("Text");
        if (data.includes("card")
            && data.includes("Img")
            && event.target.id.includes("card")
            && event.target.id.includes("Img")) {
            var tempCardId = $("#" + data).data("cardid");
            var tempCardName = $("#" + data).attr("src");
            $("#" + data).data("cardid", $("#" + event.target.id).data("cardid"));
            $("#" + data).attr("src", $("#" + event.target.id).attr("src"));
            $("#" + event.target.id).data("cardid", tempCardId);
            $("#" + event.target.id).attr("src", tempCardName);
        }
    });

    $(".card").on("click", function (e) {
        console.log(e.target);
        var cardId = $(e.target).closest(".card").data("cardid");
        console.log("PlayCard being called from Client - gameId: " + gameId + " | cardId: " + cardId);
        var promise = connection.invoke("PlayCard", gameId, cardId);
    });
    connection.on("CardPlayed", (seatNumber, cardId, cardName) => {
        resetIfFirstCardInNewTrick();
        console.log("CardPlayed called from Server - seatNumber: " + seatNumber + " | cardId: " + cardId + " | cardName: " + cardName);
        var jqueryCardObject = getGameCardBySeatNumber(seatNumber);
        $(jqueryCardObject).attr("src", "/images/Cards/" + cardName + ".png");
        $.each($(".card"), function (index, value) {
            if ($(value).data("cardid") == cardId) {
                $(value).attr("src", "/images/Cards/Empty.png");
            }
        });
        //var arrowObject = getArrowObjectBySeatNumber(seatNumber);
    });
    $(".seat").on("click", function (e) {
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
    connection.on("PlayerWagerUpdate", (seatNumber, currentWager) => {
        console.log("PlayerWagerUpdate called from Server.  SeatNumber: " + seatNumber + " | MaxWager: " + currentWager);
        var wagers = $(".wager");
        $(".wager").hide();
        if (isMySeatNumber(seatNumber)) {
            $.each(wagers, function (index, value) {
                var wagerValue = $(value).data("wager");
                if (wagerValue === 0 || wagerValue > currentWager) {
                    $(value).show();
                }
            });
            $("#wagerDiv").show();
        }
        $(".arrowImage").hide();
        var arrowObject = getArrowObjectBySeatNumber(seatNumber);
        console.log("getArrowObjectBySeatNumber called - ArrowObject: " + arrowObject);
        $(arrowObject).show();
    });
    $('.wager').click(function (e) {
        var wager = $(e.target).closest(".wager").data("wager");
        console.log("SetWager being called from Client.  GameId: " + gameId + " | WagerId: " + wager);
        connection.invoke("SetWager", gameId, wager);
        $(".wager").hide();
    });
    connection.on("PlayerTurnUpdate", (playerSeatTurn) => {
        console.log("PlayerTurnUpdate being called from Server.  PlayerSeatTurn: " + playerSeatTurn);
        $(".arrowImage").hide();
        var arrowObject = getArrowObjectBySeatNumber(playerSeatTurn);
        console.log("getArrowObjectBySeatNumber called - ArrowObject: " + $(arrowObject).attr("id"));
        $(arrowObject).show();
    });
    function isMySeatNumber(seatNumber) {
        console.log("isMySeatNumber - username: " + username + " | seatNumber: " + seatNumber + " | seatUsername: " + $(".seat[data-seatnumber='" + seatNumber + "']").find(".seatUsername").text());
        console.log();
        if ($(".seat[data-seatnumber='" + seatNumber + "']").find(".seatUsername").text() === username) {
            return true;
        }
        return false;
    }
    function getGameCardBySeatNumber(seatNumber) {
        console.log("getPositionBySeatNumber called.");
        var mySeatNumber = -1;
        $.each($(".seat"), function (index, value) {
            if ($(value).find(".seatUsername").text() === username) {
                mySeatNumber = $(value).data("seatnumber");
            }
        });
        console.log("mySeatNumber: " + mySeatNumber);
        if (mySeatNumber - seatNumber == -1 || mySeatNumber - seatNumber == 3) {
            return $("#leftPlayerCard");
        } else if (mySeatNumber - seatNumber == 2 || mySeatNumber - seatNumber == -2) {
            return $("#oppositePlayerCard");
        } else if (mySeatNumber - seatNumber == 1 || mySeatNumber - seatNumber == -3) {
            return $("#rightPlayerCard");
        } else {
            return $("#playerCard");
        }
    }
    function getArrowObjectBySeatNumber(seatNumber) {
        console.log("getArrowObjectBySeatNumber called - seatNumber: " + seatNumber);
        var mySeatNumber = -1;
        $.each($(".seat"), function (index, value) {
            if ($(value).find(".seatUsername").text() === username) {
                mySeatNumber = $(value).data("seatnumber");
            }
        });
        console.log("mySeatNumber: " + mySeatNumber);
        if (mySeatNumber - seatNumber == -1 || mySeatNumber - seatNumber == 3) {
            return $("#leftArrow");
        } else if (mySeatNumber - seatNumber == 2 || mySeatNumber - seatNumber == -2) {
            return $("#oppositeArrow");
        } else if (mySeatNumber - seatNumber == 1 || mySeatNumber - seatNumber == -3) {
            return $("#rightArrow");
        } else {
            return $("#selfArrow");
        }
    }
    function getNameObjectBySeatNumber(seatNumber) {
        console.log("getNameObjectBySeatNumber called - seatNumber: " + seatNumber);
        var mySeatNumber = -1;
        $.each($(".seat"), function (index, value) {
            if ($(value).find(".seatUsername").text() === username) {
                mySeatNumber = $(value).data("seatnumber");
            }
        });
        console.log("mySeatNumber: " + mySeatNumber);
        if (mySeatNumber - seatNumber == -1 || mySeatNumber - seatNumber == 3) {
            return $("#leftPlayerUsername");
        } else if (mySeatNumber - seatNumber == 2 || mySeatNumber - seatNumber == -2) {
            return $("#oppositePlayerUsername");
        } else if (mySeatNumber - seatNumber == 1 || mySeatNumber - seatNumber == -3) {
            return $("#rightPlayerUsername");
        } else {
            return null;
        }
    }
    function resetIfFirstCardInNewTrick() {
        if (!$("#leftPlayerCard").attr("src").includes("Empty.png")
            && !$("#oppositePlayerCard").attr("src").includes("Empty.png")
            && !$("#rightPlayerCard").attr("src").includes("Empty.png")
            && !$("#playerCard").attr("src").includes("Empty.png")) {
            $("#leftPlayerCard").attr("src", "/images/Cards/Empty.png");
            $("#oppositePlayerCard").attr("src", "/images/Cards/Empty.png");
            $("#rightPlayerCard").attr("src", "/images/Cards/Empty.png");
            $("#playerCard").attr("src", "/images/Cards/Empty.png");
        }
    }
    connection.on("GamePlayerUpdate", (gamePlayerArray) => {
        console.log("GamePlayerUpdate being called from Server.");
        $.each(gamePlayerArray, function (index, value) {
            var nameObject = getNameObjectBySeatNumber(value[0]);
            if (nameObject) {
                $(nameObject).text(value[1]);
            }
        });

    });
    connection.on("SendMessage", (message) => {
        console.log("SendMessage being called from Server");
        $('#messages').html('<p>'.concat(message).concat('</p>').concat($('#messages').html()));
    });
    connection.on("SendLog", (message) => {
        console.log("SendLog being called from Server");
        $('#logs').html('<p>'.concat(message).concat('</p>').concat($('#logs').html()));
    });
    $('#sendMessageButton').click(sendMessageButtonFunction);
    $("#playerMessage").bind("keypress", checkPlayerMessageEnterKey);

    function checkPlayerMessageEnterKey(e) {
        if (e.keyCode === 13) {
            e.preventDefault(); // Ensure it is only this code that runs
            sendMessageButtonFunction();
        }
    }

    function sendMessageButtonFunction() {
        var message = $('#playerMessage').val();
        console.log("SendChat being called from Client");
        connection.invoke("SendChat", gameId, message);
        $('#playerMessage').val('');
    }

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