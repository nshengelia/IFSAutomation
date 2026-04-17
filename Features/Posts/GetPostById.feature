Feature: Get Post by ID

  @smoke
  Scenario: Get existing post by ID
    Given a post with ID 1 exists
    When I send a GET request to /posts/1
    Then the response status code should be 200 OK
    And the response should contain post with ID 1

  Scenario: Get non-existing post
    Given a post with ID 9999 does not exist
    When I send a GET request to /posts/9999
    Then the response status code should be 404 Not Found
