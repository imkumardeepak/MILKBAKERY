﻿// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(function () {
  //$('.select2').select2();
  //$('#CodesTable').DataTable();
  //$('.select2').select2();
  $("#myDropdown1").select2();
  $("#dropcust").select2();

  // Initialize dataTable_table only if not already initialized
  if (!$.fn.DataTable.isDataTable("#dataTable_table")) {
    $("#dataTable_table").DataTable();
  }

  // Initialize dataTable1 only if not already initialized
  if (!$.fn.DataTable.isDataTable("#dataTable1")) {
    $("#dataTable1").DataTable();
  }

  $("#loaderbody").addClass("hide");

  // Initialize dataTable with specific options only if not already initialized
  if (!$.fn.DataTable.isDataTable("#dataTable")) {
    $("#dataTable").DataTable({
      info: false,
      ordering: true,
      paging: false,
      pageLength: 5,
      lengthMenu: [
        [5, 10, 20, -1],
        [5, 10, 20, "Todos"],
      ],
      dom: "Bfrtip",
      buttons: ["csv", "excel", "pdf", "print"],
    });
  }

  $(document)
    .bind("ajaxStart", function () {
      $("#loaderbody").removeClass("hide");
    })
    .bind("ajaxStop", function () {
      $("#loaderbody").addClass("hide");
    });
});

// Get the elements

var decrementBtns = document.querySelectorAll(".decrement-btn");
var incrementBtns = document.querySelectorAll(".increment-btn");
var numberInputs = document.querySelectorAll(".number-input");

// Handle the decrement button click event
decrementBtns.forEach((btn, index) => {
  btn.addEventListener("click", () => {
    let currentValue = parseFloat(numberInputs[index].value);
    if (!isNaN(currentValue)) {
      numberInputs[index].value = Math.max(currentValue - 1, 0); // Ensure the value is not less than 0
      CalcTotals();
    }
  });
});

// Handle the increment button click event
incrementBtns.forEach((btn, index) => {
  btn.addEventListener("click", () => {
    let currentValue = parseFloat(numberInputs[index].value);
    if (!isNaN(currentValue)) {
      numberInputs[index].value = currentValue + 1;
      CalcTotals();
    }
  });
});

var object = { status: false, ele: null };
function confirmation(ev) {
  if (object.status) {
    return true;
  }

  swal(
    {
      title: "Are you sure?",
      text: "You will not be able to recover this record!",
      type: "warning",

      showCancelButton: true,
      confirmButtonColor: "#3085d6",
      cancelButtonColor: "#d33",
      confirmButtonText: "Yes",
      cancelButtonText: "No",
      confirmButtonClass: "btn btn-success",
      cancelButtonClass: "btn btn-danger",
      closeOnConfirm: true,
    },
    function () {
      object.status = true;
      object.ele = ev;
      object.ele.click();
    }
  );
  return false;
}

function confirmation1(ev) {
  if (object.status) {
    return true;
  }

  swal(
    {
      title: "Are you sure?",
      text: "Are You Sure To Do Process!",
      type: "warning",

      showCancelButton: true,
      confirmButtonColor: "#3085d6",
      cancelButtonColor: "#d33",
      confirmButtonText: "Yes",
      cancelButtonText: "No",
      confirmButtonClass: "btn btn-success",
      cancelButtonClass: "btn btn-danger",
      closeOnConfirm: true,
    },
    function () {
      object.status = true;
      object.ele = ev;
      object.ele.click();
    }
  );
  return false;
}

var dp1 = $("#dp1").datepicker().data("datepicker");
var dp2 = $("#dp2").datepicker().data("datepicker");
var dp4 = $("#dp3").datepicker().data("datepicker");
var dp3 = $("#dp4").datepicker().data("datepicker");

$.ajax({
  type: "POST",
  url: "/Home/NewChart",
  contentType: "application/json; charset=utf-8",
  dataType: "json",
  success: function (chData) {
    var aData = chData;
    var aLabels = aData[0];
    var aDatasets1 = aData[1];
    var dataT = {
      labels: aLabels,
      datasets: [
        {
          label: "Amount",
          data: aDatasets1,
          fill: false,
          backgroundColor: [
            "rgba(54, 162, 235, 0.9)",
            "rgba(255, 99, 132, 0.9)",
            "rgba(255, 159, 64, 0.9)",
            "rgba(255, 205, 86, 0.9)",
            "rgba(75, 192, 192, 0.9)",
            "rgba(153, 102, 255, 0.9)",
            "rgba(201, 203, 207, 0.9)",
          ],
          borderColor: [
            "rgb(54, 162, 235)",
            "rgb(255, 99, 132)",
            "rgb(255, 159, 64)",
            "rgb(255, 205, 86)",
            "rgb(75, 192, 192)",
            "rgb(153, 102, 255)",
            "rgb(201, 203, 207)",
          ],
          borderWidth: 1,
        },
      ],
    };
    var ctx = $("#myChart").get(0).getContext("2d");
    var myNewChart = new Chart(ctx, {
      type: "bar",
      data: dataT,
      options: {
        responsive: true,
        //title: {display: true, text: 'CHART.JS DEMO CHART' },
        legend: { position: "bottom" },
        scales: {
          xAxes: [
            {
              gridLines: { display: false },
              display: true,
              scaleLabel: { display: false, labelString: "" },
            },
          ],
          yAxes: [
            {
              gridLines: { display: false },
              display: true,
              scaleLabel: { display: false, labelString: "" },
              ticks: { stepSize: 10000, beginAtZero: true },
            },
          ],
        },
      },
    });
  },
});

function getLocation() {
  if (navigator.geolocation) {
    navigator.geolocation.getCurrentPosition(showPosition);
  } else {
    alert("Geolocation is not supported by this browser.");
  }
}

function showPosition(position) {
  const latitude = document.getElementById("latitude");
  const Longitude = document.getElementById("longitute");
  const locations = document.getElementById("location");
  latitude.value = position.coords.latitude;
  Longitude.value = position.coords.longitude;
  var map = L.map("map").setView([51.505, -0.09], 13);

  L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
    attribution:
      '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
  }).addTo(map);

  // Get the user's current location using the Geolocation API
  navigator.geolocation.getCurrentPosition(function (position) {
    var userLat = position.coords.latitude;
    var userLng = position.coords.longitude;

    // Create a marker for the user's current location
    var userMarker = L.marker([userLat, userLng]).addTo(map);

    // Use Nominatim API to reverse geocode the coordinates and get the location name
    var nominatimUrl =
      "https://nominatim.openstreetmap.org/reverse?format=json&lat=" +
      userLat +
      "&lon=" +
      userLng;
    fetch(nominatimUrl)
      .then((response) => response.json())
      .then((data) => {
        var locationName = data.display_name;
        userMarker.bindPopup("You are here: " + locationName).openPopup();
        locations.value = locationName;
      });

    // Update the map view to center on the user's location
    map.setView([userLat, userLng], 13);
  });
}
function getCurrentDate() {
  const today = new Date();
  const year = today.getFullYear();
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const day = String(today.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

// Bind the current date to the input field
document.addEventListener("DOMContentLoaded", function () {
  getLocation();
  const dateField = document.getElementById("Fromdate");
  const dateField1 = document.getElementById("todate");
  dateField.value = getCurrentDate();
  dateField1.value = getCurrentDate();
});

function AddItem(btn) {
  var table;
  table = document.getElementById("CodesTable");
  var rows = table.getElementsByTagName("tr");
  var rowOuterHtml = rows[rows.length - 1].outerHTML;

  var lastrowIdx = rows.length - 2;

  var nextrowIdx = eval(lastrowIdx) + 1;

  rowOuterHtml = rowOuterHtml.replaceAll(
    "_" + lastrowIdx + "_",
    "_" + nextrowIdx + "_"
  );
  rowOuterHtml = rowOuterHtml.replaceAll(
    "[" + lastrowIdx + "]",
    "[" + nextrowIdx + "]"
  );
  rowOuterHtml = rowOuterHtml.replaceAll("-" + lastrowIdx, "-" + nextrowIdx);

  var newRow = table.insertRow();
  newRow.innerHTML = rowOuterHtml;
  $(newRow).find("select.select").select2();
  var x = document.getElementsByTagName("INPUT");

  for (var cnt = 0; cnt < x.length; cnt++) {
    if (
      x[cnt].type == "text" &&
      x[cnt].id.indexOf("_" + nextrowIdx + "_") > 0
    ) {
      if (x[cnt].id.indexOf("Unit") == 0) x[cnt].value = "";
    } else if (
      x[cnt].type == "number" &&
      x[cnt].id.indexOf("_" + nextrowIdx + "_") > 0
    )
      x[cnt].value = 0;
  }

  rebindvalidators();
}

function rebindvalidators() {
  var $form = $("#CodeSbyAnizForm");

  $form.unbind();

  $form.data("validator", null);

  $.validator.unobtrusive.parse($form);

  $form.validate($form.data("unobtrusiveValidation").options);
}

function DeleteItem(btn) {
  var table = document.getElementById("CodesTable");
  var rows = table.getElementsByTagName("tr");

  var btnIdx = btn.id.replaceAll("btnremove-", "");
  // var idOfQuantity = btnIdx + "__qty";
  // var txtQuantity = document.querySelector("[id$='" + idOfQuantity + "']");

  // txtQuantity.value = 0;

  var idOfIsDeleted = btnIdx + "__IsDeleted";
  var txtIsDeleted = document.querySelector("[id$='" + idOfIsDeleted + "']");
  txtIsDeleted.value = "true";

  $(btn).closest("tr").remove();

  //CalcTotals();
}

function DeleteItemNew(btn) {
  var table = document.getElementById("CodesTable");
  var tbody = table.querySelector("tbody");
  var btnIdx = btn.id.replace("btnremove-", "");

  // Mark the corresponding hidden IsDeleted input field as true
  var idOfIsDeleted = btnIdx + "__IsDeleted";
  var txtIsDeleted = document.querySelector("[id$='" + idOfIsDeleted + "']");
  if (txtIsDeleted) {
    txtIsDeleted.value = "true";
  }

  // Remove the row
  var row = $(btn).closest("tr");
  row.remove();

  // Update the IDs of remaining rows to keep them consistent
  var rows = tbody.getElementsByTagName("tr");
  for (var i = 0; i < rows.length; i++) {
    // Update the delete button ID
    var deleteButton = rows[i].querySelector("button[id^='btnremove-']");
    if (deleteButton) {
      deleteButton.id = "btnremove-" + i;
    }

    // Update the indices in form inputs
    var inputs = rows[i].querySelectorAll(
      "[id$='__IsDeleted'], [id$='__qty'], [id$='__phone']"
    );
    inputs.forEach((input) => {
      var name = input.name.replace(/\[\d+\]/, `[${i}]`);
      var id = input.id.replace(/\d+__/, `${i}__`);
      input.name = name;
      input.id = id;
    });
  }
}

function CalcTotals() {
  var x = document.getElementsByClassName("QtyTotal");

  var totalQty = 0;
  var Amount = 0;
  var totalAmount = 0;

  var i;

  for (i = 0; i < x.length; i++) {
    var idofIsDeleted = i + "__IsDeleted";

    var idofPrice = i + "__Rate";

    var idofTotal = i + "__Price";

    var hidIsDelId = document.querySelector("[id$='" + idofIsDeleted + "']").id;

    var priceTxtId = document.querySelector("[id$='" + idofPrice + "']").id;

    var totalTxtId = document.querySelector("[id$='" + idofTotal + "']").id;

    if (document.getElementById(hidIsDelId).value != "true") {
      totalQty = totalQty + eval(x[i].value);

      var txttotal = document.getElementById(totalTxtId);
      var txtprice = document.getElementById(priceTxtId);
      txttotal.value = (eval(x[i].value) * txtprice.value).toFixed(2);

      totalAmount = eval(totalAmount) + eval(txttotal.value);
    }
  }

  document.getElementById("txtQtyTotal").value = totalQty;
  document.getElementById("txtAmountTotal").value = totalAmount.toFixed(2);
}

document.addEventListener("focusout", function (e) {
  CalcTotals();
  var x = document.getElementsByClassName("QtyTotal");

  for (i = 0; i < x.length; i++) {
    if (eval(x[i].value) == undefined) {
      x[i].value = 0;
      CalcTotals();
    }
  }
});
document.addEventListener("change", function (e) {
  CalcTotals();
  var x = document.getElementsByClassName("QtyTotal");

  for (i = 0; i < x.length; i++) {
    if (eval(x[i].value) == undefined) {
      x[i].value = 0;
      CalcTotals();
    }
  }
});

document.addEventListener("blur", function (e) {
  CalcTotals();
  var x = document.getElementsByClassName("QtyTotal");

  for (i = 0; i < x.length; i++) {
    if (eval(x[i].value) == undefined) {
      x[i].value = 0;
      CalcTotals();
    }
  }
});

document.addEventListener("focusin", function (e) {
  var x = document.getElementsByClassName("QtyTotal");

  for (i = 0; i < x.length; i++) {
    var focusedElement = document.activeElement;
    //console.log(focusedElement);

    if (focusedElement.value == 0) {
      focusedElement.value = "";
    }
  }
});

function handleKeyDown(event) {
  if (event.key === "Enter" || event.key === "Tab") {
    event.preventDefault(); // Prevent the default behavior of the Enter and Tab keys
    const currentTextbox = event.target; // Get the current textbox element

    // Calculate totals here using CalcTotals() function

    const x = document.getElementsByClassName("QtyTotal");
    const currentIndex = Array.from(x).indexOf(currentTextbox);

    if (currentIndex >= 0 && currentIndex < x.length - 1) {
      const nextTextbox = x[currentIndex + 1];
      nextTextbox.focus();
    }
  }
}

//$(document).on("keydown", "form", function (event) {
//    return event.key != "Enter";
//});

//Purchase-order

//document.addEventListener('change', function (e) {
//    if (event.target.id.indexOf('ProductName') >= 0) {
//        var tid = event.target.id;

//        var product = document.getElementById(tid).value;
//        console.log(product);

//        var txtProductCodeId = tid.replaceAll('ProductName', 'ProductCode');
//        var txtProductCode = document.getElementById(txtProductCodeId);

//        var txtUnitId = tid.replaceAll('ProductName', 'Unit');
//        var txtUnit = document.getElementById(txtUnitId);

//        var txtRateId = tid.replaceAll('ProductName', 'Rate');
//        var txtRate = document.getElementById(txtRateId);
//        txtProductCode.value = null;
//        txtUnit.value = null;
//        txtRate.value = null;
//        // Make an AJAX request to the controller action
//        $.ajax({
//            url: '/PurchaseOrders/ProductSelect',
//            type: 'POST',
//            data: { optionValue: product },
//            success: function (result) {
//                console.log(result);
//                $.each(result, function (key, data) {
//                    txtProductCode.value = data.material3partycode;
//                    txtUnit.value = data.unit;
//                    txtRate.value = parseInt(data.price).toFixed(2);
//                });
//            },
//            error: function (xhr, status, error) {
//                // Handle the error response
//                console.log("An error occurred while executing the action.");
//            }
//        });

//    }

//}, false);

const tableContainer = document.querySelector(".table-container");

// Add scroll event listener to the table container
tableContainer.addEventListener("scroll", function () {
  const tableHeader = this.querySelector("thead");
  const isSticky = tableHeader.classList.contains("sticky");
  const scrollTop = this.scrollTop;

  // If the table container is scrolled beyond the table header, add the sticky class
  if (scrollTop > 0 && !isSticky) {
    tableHeader.classList.add("sticky");
  } else if (scrollTop === 0 && isSticky) {
    // If the table container is scrolled back to the top, remove the sticky class
    tableHeader.classList.remove("sticky");
  }
});
