﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Set new default font family and font color to mimic Bootstrap's default styling
Chart.defaults.global.defaultFontFamily =
  '-apple-system,system-ui,BlinkMacSystemFont,"Segoe UI",Roboto,"Helvetica Neue",Arial,sans-serif';
Chart.defaults.global.defaultFontColor = "#292b2c";

// Pie Chart Example
$.ajax({
  url: "/Home/CratesOutwardChart",
  type: "POST",
  dataType: "json",
  success: function (data) {
    console.log(data);
    var labels = data[0];
    var values = data[1];
    var ctx = document.getElementById("myPieChart");
    var myPieChart = new Chart(ctx, {
      type: "pie",
      data: {
        labels: labels,
        datasets: [
          {
            data: values,
            backgroundColor: [
              "#4e73df",
              "#1cc88a",
              "#36b9cc",
              "#f6c23e",
              "#e74a3b",
              "#858796",
              "#5a5c69",
              "#d45d79",
            ],
          },
        ],
      },
      options: {
        tooltips: {
          callbacks: {
            label: function (tooltipItem, data) {
              var dataset = data.datasets[tooltipItem.datasetIndex];
              var total = dataset.data.reduce(function (
                previousValue,
                currentValue,
                currentIndex,
                array
              ) {
                return previousValue + currentValue;
              });
              var currentValue = dataset.data[tooltipItem.index];
              var percentage = Math.floor((currentValue / total) * 100 + 0.5);
              return (
                data.labels[tooltipItem.index] +
                ": " +
                currentValue +
                " (" +
                percentage +
                "%)"
              );
            },
          },
        },
      },
    });
  },
  error: function (error) {
    console.log(error);
  },
});

$.ajax({
  url: "/Home/NewChart",
  type: "POST",
  dataType: "json",
  success: function (chData) {
    var aData = chData;
    var aLabels = aData[0];
    var aDatasets1 = aData[1];
    var ctx = document.getElementById("myAreaChart");
    var myLineChart = new Chart(ctx, {
      type: "line",
      data: {
        labels: aLabels,
        datasets: [
          {
            label: "Earnings",
            lineTension: 0.3,
            backgroundColor: "rgba(2,117,216,0.2)",
            borderColor: "rgba(2,117,216,1)",
            pointRadius: 5,
            pointBackgroundColor: "rgba(2,117,216,1)",
            pointBorderColor: "rgba(255,255,255,0.8)",
            pointHoverRadius: 5,
            pointHoverBackgroundColor: "rgba(2,117,216,1)",
            pointHitRadius: 50,
            pointBorderWidth: 2,
            data: aDatasets1,
          },
        ],
      },
      options: {
        scales: {
          xAxes: [
            {
              time: {
                unit: "date",
              },
              gridLines: {
                display: false,
              },
              ticks: {
                maxTicksLimit: 7,
              },
            },
          ],
          yAxes: [
            {
              ticks: {
                min: 0,
                maxTicksLimit: 5,
              },
              gridLines: {
                color: "rgba(0, 0, 0, .125)",
              },
            },
          ],
        },
        legend: {
          display: false,
        },
      },
    });
  },
  error: function (error) {
    console.log(error);
  },
});

$(function () {
  // Initialize select2
  if ($.fn.select2) {
    $(".select2").select2();
  }
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
  if (numberInputs[index]) {
    // Check if numberInputs[index] exists
    btn.addEventListener("click", () => {
      let currentValue = parseFloat(numberInputs[index].value);
      if (!isNaN(currentValue)) {
        numberInputs[index].value = Math.max(currentValue - 1, 0); // Ensure the value is not less than 0
        CalcTotals();
      }
    });
  }
});

// Handle the increment button click event
incrementBtns.forEach((btn, index) => {
  if (numberInputs[index]) {
    // Check if numberInputs[index] exists
    btn.addEventListener("click", () => {
      let currentValue = parseFloat(numberInputs[index].value);
      if (!isNaN(currentValue)) {
        numberInputs[index].value = currentValue + 1;
        CalcTotals();
      }
    });
  }
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

// Custom confirmation dialog for delete operations
function confirmDelete(ev) {
  if (object.status) {
    return true;
  }

  swal(
    {
      title: "Are you sure?",
      text: "Are you sure you want to delete this item?",
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
    // Check if myChart element exists before accessing its context
    var myChartElement = $("#myChart");
    if (myChartElement.length > 0 && myChartElement.get(0)) {
      var ctx = myChartElement.get(0).getContext("2d");
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
    }
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
  if (latitude && Longitude && locations) {
    // Check if elements exist
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
  if (dateField) dateField.value = getCurrentDate();
  if (dateField1) dateField1.value = getCurrentDate();
});

function AddItem(btn) {
  var table = document.getElementById("CodesTable");
  if (!table) return; // Check if table exists

  var tbody = table.querySelector("tbody");
  var rows = tbody.getElementsByTagName("tr");
  
  // Count only visible rows to determine the next index
  var visibleRowCount = 0;
  for (var i = 0; i < rows.length; i++) {
    if (rows[i].style.display !== "none") {
      visibleRowCount++;
    }
  }
  
  var nextrowIdx = visibleRowCount;

  // Determine if this is for EmpToCustMap or Cust2CustMap based on the page URL
  var isEmpToCustMap = window.location.pathname.includes("EmpToCustMaps");
  var collectionName = isEmpToCustMap ? "Cust2EmpMaps" : "Mappedcusts";

  // Create a new row with proper structure
  var newRow = tbody.insertRow();
  newRow.innerHTML = `
    <td style="width:300px">
      <select name="${collectionName}[${nextrowIdx}].customer" id="${collectionName}_${nextrowIdx}__customer" class="form-control" onchange="handleSelectChange(this)">
        <option value="">--Select Customer--</option>
      </select>
      <span class="text-danger field-validation-valid" data-valmsg-for="${collectionName}[${nextrowIdx}].customer" data-valmsg-replace="true"></span>
    </td>
    <td style="width:200px">
      <input class="form-control" type="text" name="${collectionName}[${nextrowIdx}].phone" id="${collectionName}_${nextrowIdx}__phone" readonly>
      <span class="text-danger field-validation-valid" data-valmsg-for="${collectionName}[${nextrowIdx}].phone" data-valmsg-replace="true"></span>
      <input type="hidden" name="${collectionName}[${nextrowIdx}].IsDeleted" id="${collectionName}_${nextrowIdx}__IsDeleted" value="false">
    </td>
    <td>
      <button id='btnremove-${nextrowIdx}' type="button" class="btn text-white text-[16px] hover:bg-red-700 bg-red-600 visible" onclick="DeleteItemNew(this)">
        Delete
      </button>
    </td>
  `;

  // Copy options from the first select element if it exists
  var firstSelect = tbody.querySelector("select[name*='customer']");
  var newSelect = newRow.querySelector("select");
  
  if (firstSelect && newSelect) {
    // Copy all options from the first select to the new select
    for (var i = 0; i < firstSelect.options.length; i++) {
      var option = firstSelect.options[i];
      var newOption = new Option(option.text, option.value, option.defaultSelected, option.selected);
      newSelect.add(newOption);
    }
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
  if (!table) return; // Check if table exists

  var rows = table.getElementsByTagName("tr");

  var btnIdx = btn.id.replaceAll("btnremove-", "");
  // var idOfQuantity = btnIdx + "__qty";
  // var txtQuantity = document.querySelector("[id$='" + idOfQuantity + "']");

  // txtQuantity.value = 0;

  var idOfIsDeleted = btnIdx + "__IsDeleted";
  var txtIsDeleted = document.querySelector("[id$='" + idOfIsDeleted + "']");
  if (txtIsDeleted) txtIsDeleted.value = "true";

  $(btn).closest("tr").remove();

  //CalcTotals();
}

function DeleteItemNew(btn) {
  var table = document.getElementById("CodesTable");
  if (!table) return; // Check if table exists

  var tbody = table.querySelector("tbody");
  var btnIdx = btn.id.replace("btnremove-", "");

  // Determine if this is for EmpToCustMap or Cust2CustMap based on the page URL
  var isEmpToCustMap = window.location.pathname.includes("EmpToCustMaps");
  var collectionName = isEmpToCustMap ? "Cust2EmpMaps" : "Mappedcusts";

  // Mark the corresponding hidden IsDeleted input field as true
  var idOfIsDeleted = collectionName + "_" + btnIdx + "__IsDeleted";
  var txtIsDeleted = document.getElementById(idOfIsDeleted);
  if (txtIsDeleted) {
    txtIsDeleted.value = "true";
  }

  // Hide the row instead of removing it to maintain form collection indices
  var row = $(btn).closest("tr");
  row.hide();

  // Update the IDs of remaining visible rows to keep them consistent
  var rows = tbody.getElementsByTagName("tr");
  var visibleRowIndex = 0;
  
  for (var i = 0; i < rows.length; i++) {
    // Only process visible rows
    if (rows[i].style.display !== "none") {
      // Update the delete button ID
      var deleteButton = rows[i].querySelector("button[id^='btnremove-']");
      if (deleteButton) {
        deleteButton.id = "btnremove-" + visibleRowIndex;
      }

      // Update the indices in form inputs
      var inputs = rows[i].querySelectorAll("input, select");
      inputs.forEach((input) => {
        if (input.name) {
          // Update name attributes
          input.name = input.name.replace(/\[\d+\]/, "[" + visibleRowIndex + "]");
        }
        if (input.id) {
          // Update id attributes
          input.id = input.id.replace(/_\d+__/g, collectionName + "_" + visibleRowIndex + "__");
        }
      });
      
      visibleRowIndex++;
    }
  }
}

function CalcTotals() {
  var x = document.getElementsByClassName("QtyTotal");
  if (!x || x.length === 0) return; // Check if elements exist

  var totalQty = 0;
  var Amount = 0;
  var totalAmount = 0;

  var i;

  for (i = 0; i < x.length; i++) {
    var idofIsDeleted = i + "__IsDeleted";

    var idofPrice = i + "__Rate";

    var idofTotal = i + "__Price";

    var hidIsDelId = document.querySelector("[id$='" + idofIsDeleted + "']");
    var priceTxtId = document.querySelector("[id$='" + idofPrice + "']");
    var totalTxtId = document.querySelector("[id$='" + idofTotal + "']");

    // Check if elements exist before accessing their properties
    if (hidIsDelId && priceTxtId && totalTxtId) {
      if (hidIsDelId.value != "true") {
        totalQty = totalQty + eval(x[i].value);

        var txttotal = document.getElementById(totalTxtId.id);
        var txtprice = document.getElementById(priceTxtId.id);
        if (txttotal && txtprice) {
          txttotal.value = (eval(x[i].value) * txtprice.value).toFixed(2);
          totalAmount = eval(totalAmount) + eval(txttotal.value);
        }
      }
    }
  }

  // Check if total elements exist before setting their values
  var txtQtyTotal = document.getElementById("txtQtyTotal");
  var txtAmountTotal = document.getElementById("txtAmountTotal");

  if (txtQtyTotal) txtQtyTotal.value = totalQty;
  if (txtAmountTotal) txtAmountTotal.value = totalAmount.toFixed(2);
}

document.addEventListener("focusout", function (e) {
  CalcTotals();
  var x = document.getElementsByClassName("QtyTotal");
  if (!x) return; // Check if elements exist

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
  if (!x) return; // Check if elements exist

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
  if (!x) return; // Check if elements exist

  for (i = 0; i < x.length; i++) {
    if (eval(x[i].value) == undefined) {
      x[i].value = 0;
      CalcTotals();
    }
  }
});

document.addEventListener("focusin", function (e) {
  var x = document.getElementsByClassName("QtyTotal");
  if (!x) return; // Check if elements exist

  for (i = 0; i < x.length; i++) {
    var focusedElement = document.activeElement;
    //console.log(focusedElement);

    if (focusedElement && focusedElement.value == 0) {
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
    if (!x) return; // Check if elements exist

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

function handleSelectChange(selectElement) {
  var tid = selectElement.id;
  var product = selectElement.value;

  // Check if this is for the main employee/customer dropdown (myDropdown1)
  if (tid === "myDropdown1") {
    // Determine which controller to use based on the page context
    // We can determine this by checking if we're on EmpToCustMaps or Cust2CustMap page
    var path = window.location.pathname;
    var controller = path.includes("EmpToCustMaps") ? "EmpToCustMaps" : "Cust2CustMap";
    
    if (product !== "") {
      $.ajax({
        url: '/' + controller + '/fill_form',
        type: 'GET',
        dataType: 'json',
        data: { selectedvalue: product },
        success: function (data) {
          document.getElementById("phone").value = data;
        }
      });
    } else {
      document.getElementById("phone").value = "";
    }
    return;
  }

  // Check if this is for customer selection in dynamic table rows
  var isEmpToCustMap = tid.includes("Cust2EmpMaps");
  
  // For EmpToCustMap, check for duplicate selection
  if (isEmpToCustMap) {
    var allSelectElements = document.querySelectorAll("select[id*='customer']");
    var isDuplicate = false;

    allSelectElements.forEach(function (otherSelect) {
      if (otherSelect.id !== tid && otherSelect.value === product) {
        isDuplicate = true;
      }
    });

    if (isDuplicate) {
      alert("This customer has already been selected in another row.");
      selectElement.value = ""; // Reset the selection
      return;
    }
  }

  // Proceed to update the phone field for customer selection in dynamic table rows
  var txtProductCodeId = tid.replaceAll('customer', 'phone');
  var txtProductCode = document.getElementById(txtProductCodeId);

  if (txtProductCode) {
    txtProductCode.value = null;

    // Make AJAX call to fetch customer data
    // For dynamic table rows, we always fetch customer phone numbers from Cust2CustMapController
    // because we're always looking up customer phone numbers, not employee phone numbers
    $.ajax({
      url: '/Cust2CustMap/fill_form',
      type: 'GET',
      dataType: 'json',
      data: { selectedvalue: product },
      success: function (data) {
        if (txtProductCode) {
          txtProductCode.value = data;
        }
      }
    });
  }
}

// Check if tableContainer exists before adding event listener
const tableContainer = document.querySelector(".table-container");
if (tableContainer) {
  // Add scroll event listener to the table container
  tableContainer.addEventListener("scroll", function () {
    const tableHeader = this.querySelector("thead");
    if (tableHeader) {
      const isSticky = tableHeader.classList.contains("sticky");
      const scrollTop = this.scrollTop;

      // If the table container is scrolled beyond the table header, add the sticky class
      if (scrollTop > 0 && !isSticky) {
        tableHeader.classList.add("sticky");
      } else if (scrollTop === 0 && isSticky) {
        // If the table container is scrolled back to the top, remove the sticky class
        tableHeader.classList.remove("sticky");
      }
    }
  });
}
