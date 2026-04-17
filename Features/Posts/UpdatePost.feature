Feature: Update Post

  @smoke
  Scenario: Update an existing post
    Given a post with ID 1 exists
    And I have updated post data
    When I send a PUT request to /posts/1
    Then the response status code should be 200 OK
    And the response body should reflect the updated data
