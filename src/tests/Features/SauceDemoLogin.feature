Feature: Saucedemo Login
	This feature file contains tests related to login
	1. Valid login should take to inventory page
	2. Invalid credentials should show error message 
    3. Problem user should see same images on inventory page

        @sauce
        Scenario: Valid login should take to inventory page
            Given the user is on login page
             When the user logged in with username and password
             Then the user is navigated to Inventory Page
        

        @sauce
        Scenario Outline: Invalid credentials should show error message
            Given the user is on login page
             When the user logged in with "<UserName>" and "<Password>"
             Then the user is shown error message

        Examples:
                  | UserName          | Password         |
                  | locked_out_user   | secret_sauce     |
                  | non_existing_user | secret_sauce     |
                  | standard_user     | invalid_password |
             

        @sauce
        Scenario: Problem user should see same images on inventory page
            Given the user is on login page
             When the user logged in with "problem_user" and "secret_sauce"
             Then the user is navigated to Inventory Page
             Then the product images are all invalid