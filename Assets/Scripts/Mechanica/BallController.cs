using UnityEngine;
using System.Collections;

public class BallController : MonoBehaviour
{
// Movenment variables
    public Transform spawnpoint;
    public float spawnpontDistance = 0.02f;
    private Vector3 curPosition;
    private Vector3 screenPoint;
    private Vector3 offset;
    private Vector3 restScale;

// Throw variables
    public GameObject objectToThrow;
    public Transform newParent;
    public Transform throwSpawnpoint; // this object
    public float throwForce = 0.001f;
    private Vector2 startSwipeMousePosition;
    private Vector2 lastSwipeMousePosition;

    private float swipeStartTime;
    private float swipeEndTime;
    private GameObject thrownObject;

    // Boolean variables
    private bool reset;
    private bool throwUp;

    // Use this for initialization
    private void Start()
	{
        restScale = transform.localScale;

        reset = true;
        throwUp = false;
    }

	// Update is called once per frame
	private void Update()
	{
        Reset(reset);

        if (throwUp)
        {
            Throw(startSwipeMousePosition, lastSwipeMousePosition);

            reset = true;
            throwUp = false;
        }
    }

    // Called when the mouse button is released on the ball
    private void OnMouseUp()
    {
    // Movenment
        // Check if the ball is near the start position
        if (Vector3.Distance(transform.position, spawnpoint.transform.position) < spawnpontDistance)
        {
            reset = true;
            throwUp = false;
        }

    // Trow
        else
        {
            reset = false;
            throwUp = true;
        }
        // Last Mouse Position
        lastSwipeMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        swipeEndTime = Time.time;

    }

    // Called when the mouse button is pressed on the ball
    private void OnMouseDown()
    {
    // Movenment
        // Get the distance between the camera and the object
        screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);

        // Calculate the offset between the mouse position and the object position
        offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));

    // Throw
        // Start swipe position
        startSwipeMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

        swipeStartTime = Time.time;


        reset = false;
        throwUp = false;
    }

    // Called when the mouse is dragged over the ball
    private void OnMouseDrag()
    {
    // Movenment
        // Calculate the new position of the object based on the mouse position and the offset
        Vector3 mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        curPosition = Camera.main.ScreenToWorldPoint(mousePosition) + offset;

        // Move the object to the new position
        transform.position = curPosition;
    }

    private void Throw(Vector2 startPos, Vector2 endPos)
    {
        // Hide 2d space's object (ball)
        gameObject.SetActive(false);

        // Instantiate the object to throw
        thrownObject = Instantiate(objectToThrow, throwSpawnpoint.transform.position, objectToThrow.transform.rotation);
        //thrownObject.transform.position = new Vector3(thrownObject.transform.position.x, thrownObject.transform.position.y, 5f);

        // Set the new parent of the game object
        thrownObject.transform.SetParent(newParent);

        // Calculate the time it took to make the swipe by subtracting the swipeStartTime from the swipeEndTime
        float swipeTime = swipeEndTime - swipeStartTime;

        // Calculate the distance the swipe traveled by subtracting the startSwipeMousePosition from the lastSwipeMousePosition
        Vector2 swipeDistance;
        swipeDistance.y = (endPos.y - startPos.y) / Screen.width * 100;
        swipeDistance.x = Mathf.Abs(endPos.x - startPos.x) / Screen.width * 100 * ((endPos.x / Screen.width) - (startPos.x / Screen.width));

        // Calculate the swipe direction to throw the object
        Vector3 throwDirection = new Vector3(swipeDistance.x, 50f, swipeDistance.y);
        throwDirection = Camera.main.transform.TransformDirection(throwDirection);

        // Calculate the speed of the swipe by dividing the distance by the time:
        float swipeSpeed = swipeDistance.magnitude / swipeTime;

        // Use the swipeSpeed to modify the throwForce variable:
        float modifiedThrowForce = throwForce * swipeSpeed;

        // Use the modifiedThrowForce variable to apply the throw force to the object
        thrownObject.GetComponent<Rigidbody>().AddForce((throwDirection * modifiedThrowForce / 2) + (Vector3.up * throwForce), ForceMode.Impulse);

        // Destroy throwing object after 10 seconds
        Destroy(thrownObject, 2f);
    }

    public void Reset(bool rest)
    {
        if (rest)
        {
            // Return ball to start position
            transform.position = spawnpoint.transform.position;

            // Set scale back to default
            transform.localScale = restScale;

            // Show 2d space's object (ball)
            gameObject.SetActive(true);

            // Destroy throwing object
            Destroy(thrownObject, 0.5f);
        }
        else
        {
            // Increase scale by 20%
            transform.localScale = restScale * 1.2f;
        }
    }
}
