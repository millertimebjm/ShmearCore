// python3 -m http.server 8079
var gameId = 0;
var username = '';

$().ready(function () {
    var urlParams = new URLSearchParams(window.location.search);
    username = urlParams.get('name');
    if (!username || username === '') {
        window.location = "index.html";
    }

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
            console.log("find seatusername=" + $(value).find(".seatUsername").text());
            if (seatUsername === "") {
                seatUsername = "Open";
                $(value).parent().find(".addComputerSeat").show();
                $(value).parent().find(".dropComputerSeat").hide();
            }
            else if (seatUsername.startsWith("Comp")) {
                $(value).parent().find(".addComputerSeat").hide();
                $(value).parent().find(".dropComputerSeat").show();
            } else {
                $(value).parent().find(".addComputerSeat").hide();
                $(value).parent().find(".dropComputerSeat").hide();
            }
            $(value).find(".seatUsername").text(seatUsername);
        });
        gameId = openGameId;
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
        var cardId = parseInt($(e.target).closest(".card").data("cardid"));
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
    });
    $(".seat").hover(function () {
        $(this).css('cursor', 'pointer');
    });
    $(".seat").on("click", function (e) {
        console.log(e.target);
        var seatNumber = $(e.target).closest(".seat").data("seatnumber");
        console.log("SetSeatStatus being called from Client.  GameId: " + gameId + " | seatNumber: " + seatNumber);
        var promise = connection.invoke("SetSeatStatus", gameId, seatNumber);
    });
    $(".addComputerSeat").on("click", function (e) {
        console.log(e.target);
        var seatNumber = $(e.target).data("computerseatid");
        console.log("SetComputerSeatStatus being called from Client.  GameId: " + gameId + " | computerSeatNumber: " + seatNumber);
        var promise = connection.invoke("SetComputerSeatStatus", gameId, seatNumber);
    });
    $(".dropComputerSeat").on("click", function (e) {
        console.log(e.target);
        var seatNumber = $(e.target).data("computerseatid");
        console.log("SetComputerSeatStatus being called from Client.  GameId: " + gameId + " | computerSeatNumber: " + seatNumber);
        var promise = connection.invoke("SetComputerSeatStatus", gameId, seatNumber);
    });
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
});
