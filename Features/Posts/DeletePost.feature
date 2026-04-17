Feature: Delete Post

  @smoke
  Scenario: Delete an existing post
    Given a post with ID 1 exists
    When I send a DELETE request to /posts/1
    Then the response status code should be 200 OK
