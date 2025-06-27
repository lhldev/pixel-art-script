import pyautogui


def get_mouse_location():
    try:
        while True:
            x, y = pyautogui.position()
            print("                              ", end='\r')
            print(f"Mouse position: x={x}, y={y}", end='\r')
    except KeyboardInterrupt:
        print("\nScript terminated by the user.")


if __name__ == "__main__":
    print("Press Ctrl+C to terminate the script.")
    get_mouse_location()
