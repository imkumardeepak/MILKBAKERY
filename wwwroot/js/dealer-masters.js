// Dealer Masters specific JavaScript functionality
(function () {
  // Wait for DOM to be fully loaded
  document.addEventListener("DOMContentLoaded", function () {
    // Initialize variables
    const customerSelect = document.getElementById("customerSelect");
    const routeCodeInput = document.getElementById("routeCode");
    const dealerForm = document.getElementById("dealerForm");
    const selectedCountElement = document.getElementById("selectedCount");
    const totalQuantityElement = document.getElementById("totalQuantity");

    // Check if we're on the correct page
    if (!dealerForm) {
      return; // Not on the dealer masters page, exit
    }

    // Update counts on page load
    try {
      updateCounts();
    } catch (error) {
      console.warn("Error updating counts on load:", error);
    }

    // Customer selection change event
    if (customerSelect) {
      customerSelect.addEventListener("change", function () {
        const customerId = this.value;

        if (customerId) {
          // Make AJAX call to get route code
          fetch(`/DealerMasters/GetRouteCode?customerId=${customerId}`)
            .then((response) => {
              if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
              }
              return response.json();
            })
            .then((data) => {
              if (routeCodeInput) {
                routeCodeInput.value = data.routeCode || "";
              }
            })
            .catch((error) => {
              console.error("Error fetching route code:", error);
              if (routeCodeInput) {
                routeCodeInput.value = "";
              }
            });
        } else {
          if (routeCodeInput) {
            routeCodeInput.value = "";
          }
        }
      });
    }

    // Function to handle checkbox changes
    function handleCheckboxChange(checkbox) {
      if (!checkbox) return;

      const materialId = checkbox.value;
      const quantityInput = document.querySelector(
        `input[name="MaterialQuantities[${materialId}]"]`
      );

      if (checkbox.checked) {
        // Enable quantity input
        if (quantityInput) {
          quantityInput.disabled = false;
          if (
            parseInt(quantityInput.value) === 0 ||
            isNaN(parseInt(quantityInput.value))
          ) {
            quantityInput.value = "1";
          }
        }
      } else {
        // Disable quantity input
        if (quantityInput) {
          quantityInput.value = "0";
          quantityInput.disabled = true;
        }
      }
      updateCounts();
    }

    // Event delegation for checkbox changes
    document.addEventListener("change", function (e) {
      if (e.target && e.target.classList.contains("material-checkbox")) {
        try {
          handleCheckboxChange(e.target);
        } catch (error) {
          console.error("Error handling checkbox change:", error);
        }
      }
    });

    // Function to handle quantity changes
    function handleQuantityChange(quantityInput, materialId) {
      if (!quantityInput) return;

      const value = parseInt(quantityInput.value) || 0;
      const checkbox = document.querySelector(
        `input[name="SelectedMaterialIds"][value="${materialId}"]`
      );

      // Ensure value is not negative
      if (value < 0) {
        quantityInput.value = 0;
        value = 0;
      }

      // Auto-check/uncheck checkbox based on quantity
      if (checkbox) {
        if (value > 0 && !checkbox.checked) {
          checkbox.checked = true;
          handleCheckboxChange(checkbox);
        } else if (value === 0 && checkbox.checked) {
          checkbox.checked = false;
          handleCheckboxChange(checkbox);
        }
      }

      updateCounts();
    }

    // Event delegation for input changes (for direct input)
    document.addEventListener("input", function (e) {
      if (e.target && e.target.classList.contains("material-quantity")) {
        const materialId = e.target.getAttribute("data-material-id");
        try {
          handleQuantityChange(e.target, materialId);
        } catch (error) {
          console.error("Error handling quantity change:", error);
        }
      }
    });

    // Handle focus event for quantity inputs to clear value when user focuses on a non-zero field
    document.addEventListener(
      "focus",
      function (e) {
        if (e.target && e.target.classList.contains("material-quantity")) {
          // If the field has a value and user focuses on it, clear it for easy re-entry
          if (e.target.value !== "" && parseInt(e.target.value) > 0) {
            // Clear the value when user focuses on a field with existing value
            e.target.value = "";
          }
        }
      },
      true
    ); // Using capture phase to ensure we catch the focus event

    // Handle keydown events for quantity inputs (Enter)
    document.addEventListener("keydown", function (e) {
      if (e.target && e.target.classList.contains("material-quantity")) {
        // Prevent form submission and move to next field when Enter is pressed
        if (e.key === "Enter") {
          e.preventDefault(); // Prevent form submission

          // Find all quantity inputs
          const quantityInputs = Array.from(
            document.querySelectorAll(".material-quantity")
          );

          // Find current input index
          const currentIndex = quantityInputs.indexOf(e.target);

          // Move to next input if available, otherwise remove focus
          if (currentIndex < quantityInputs.length - 1) {
            // Move to next quantity input
            const nextInput = quantityInputs[currentIndex + 1];
            setTimeout(() => {
              nextInput.focus();
            }, 10);
          } else {
            // If this is the last input, remove focus
            setTimeout(() => {
              e.target.blur(); // Remove focus from the input
            }, 10);
          }
        }
      }
    });

    // Function to handle increment button clicks
    function handleIncrement(materialId) {
      const quantityInput = document.querySelector(
        `input[name="MaterialQuantities[${materialId}]"]`
      );
      const checkbox = document.querySelector(
        `input[name="SelectedMaterialIds"][value="${materialId}"]`
      );

      if (quantityInput) {
        let quantity = parseInt(quantityInput.value) || 0;
        quantity = quantity + 1;
        quantityInput.value = quantity;

        // Auto-check checkbox if not already checked
        if (checkbox && !checkbox.checked) {
          checkbox.checked = true;
          handleCheckboxChange(checkbox);
        } else {
          try {
            handleQuantityChange(quantityInput, materialId);
          } catch (error) {
            console.error("Error handling quantity change:", error);
          }
        }
      }
    }

    // Function to handle decrement button clicks
    function handleDecrement(materialId) {
      const quantityInput = document.querySelector(
        `input[name="MaterialQuantities[${materialId}]"]`
      );

      if (quantityInput) {
        let quantity = parseInt(quantityInput.value) || 0;
        if (quantity > 0) {
          quantity = quantity - 1;
          quantityInput.value = quantity;
          try {
            handleQuantityChange(quantityInput, materialId);
          } catch (error) {
            console.error("Error handling quantity change:", error);
          }
        }
      }
    }

    // Event delegation for button clicks
    document.addEventListener("click", function (e) {
      // Handle increment button clicks
      if (e.target && e.target.classList.contains("increment-btn")) {
        const materialId = e.target.getAttribute("data-material-id");
        try {
          handleIncrement(materialId);
        } catch (error) {
          console.error("Error handling increment:", error);
        }
      }

      // Handle decrement button clicks
      if (e.target && e.target.classList.contains("decrement-btn")) {
        const materialId = e.target.getAttribute("data-material-id");
        try {
          handleDecrement(materialId);
        } catch (error) {
          console.error("Error handling decrement:", error);
        }
      }
    });

    // Form validation before submit
    if (dealerForm) {
      dealerForm.addEventListener("submit", function (e) {
        try {
          // Check if at least one material is selected
          const selectedMaterials = document.querySelectorAll(
            'input[name="SelectedMaterialIds"]:checked'
          );
          if (selectedMaterials.length === 0) {
            e.preventDefault();
            alert(
              "No Materials Selected\nPlease select at least one material."
            );
            return false;
          }

          // Check if at least one selected material has quantity > 0
          let hasValidQuantity = false;
          selectedMaterials.forEach(function (checkbox) {
            const materialId = checkbox.value;
            const quantityInput = document.querySelector(
              `input[name="MaterialQuantities[${materialId}]"]`
            );
            const quantity =
              parseInt(quantityInput ? quantityInput.value : 0) || 0;

            if (quantity > 0) {
              hasValidQuantity = true;
            }
          });

          if (!hasValidQuantity) {
            e.preventDefault();
            alert(
              "Invalid Quantity\nAt least one selected material must have a quantity greater than 0."
            );
            return false;
          }

          return true;
        } catch (error) {
          console.error("Error during form submission:", error);
          // Allow form submission to proceed in case of script error
          return true;
        }
      });
    }

    // Function to update selected count and total quantity
    function updateCounts() {
      try {
        const selectedMaterials = document.querySelectorAll(
          'input[name="SelectedMaterialIds"]:checked'
        );
        const selectedCount = selectedMaterials.length;

        let totalQuantity = 0;
        selectedMaterials.forEach(function (checkbox) {
          const materialId = checkbox.value;
          const quantityInput = document.querySelector(
            `input[name="MaterialQuantities[${materialId}]"]`
          );
          const quantity =
            parseInt(quantityInput ? quantityInput.value : 0) || 0;
          totalQuantity += quantity;
        });

        // Update the display elements
        if (selectedCountElement) {
          selectedCountElement.textContent = selectedCount;
        }
        if (totalQuantityElement) {
          totalQuantityElement.textContent = totalQuantity;
        }
      } catch (error) {
        console.error("Error updating counts:", error);
      }
    }

    // Initialize checkboxes on page load with better error handling
    setTimeout(function () {
      const checkboxes = document.querySelectorAll(".material-checkbox");
      checkboxes.forEach(function (checkbox) {
        try {
          handleCheckboxChange(checkbox);
        } catch (error) {
          console.warn("Error initializing checkbox:", error);
        }
      });
    }, 100);
  });
})();
