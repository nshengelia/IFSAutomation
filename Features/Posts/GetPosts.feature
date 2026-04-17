Feature: Posts API Testing

  @smoke
  Scenario: Get all posts successfully
    Given the API base URL is configured
    When I send a GET request to /posts
    Then the response status code should be 200 OK
    And the response should contain a list of posts

  Scenario: Verify number of posts
    Given the API base URL is configured
    When I send a GET request to /posts
    Then the response should contain exactly 100 posts

  Scenario: Verify post structure
    Given the API base URL is configured
    When I send a GET request to /posts
    Then each post should contain the following fields:
      | id     |
      | userId |
      | title  |
      | body   |
