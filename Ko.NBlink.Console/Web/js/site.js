/* - Demo site code  only - */
(function () {
    var burger = document.querySelector('.burger');
    var menu = document.querySelector('#' + burger.dataset.target);
    burger.addEventListener('click', function () {
        burger.classList.toggle('is-active');
        menu.classList.toggle('is-active');
    });
})();
function openTab(c, e) {
    var a;var b = document.getElementsByClassName("content-tab");
    for (a = 0; a < b.length; a++) {
        b[a].style.display = "none";
    }
    var d = document.getElementsByClassName("tab");
    for (a = 0; a < b.length; a++) {
        d[a].className = d[a].className.replace(" is-active", "");
    }
    document.getElementById(e).style.display = "block";
    c.currentTarget.className += " is-active";
}


/*Chart JS */


var chartdata = {
    labels: ["Sql Server", "Visual Studio", "Chrome",
        "DllHost", "node.exe", "postgres.exe", "ServiceHub", "MSWord"],
    datasets: [
        {
            label: "Memory Usage",
            backgroundColor: "rgba(68,181,238,0.2)",
            borderColor: "rgba(68,181,125,1)",
            pointBackgroundColor: "rgba(179,181,198,1)",
            pointBorderColor: "#EF4242",
            pointHoverBackgroundColor: "#fff",
            pointHoverBorderColor: "rgba(179,181,198,1)",
            data: [10, 9, 8, 7, 6, 5, 4, 3]
        }
    ]
};

var ctx = document.getElementById("radarChart");
var memusageChart = new Chart(ctx, {
    type: 'radar',
    data: chartdata,
    options: {
        scale: {
            ticks: {
                beginAtZero: true
            }
        }
    }
});