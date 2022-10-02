// Specify all the using statements which give us the access to all the APIs that you'll need
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.Ports;
using System.Numerics.Tensors;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ModelDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// // All the required variable declaration
    
    public sealed partial class MainPage : Page
    {
        private classifierModel modelGen;
        private classifierInput input = new classifierInput();
        private classifierOutput output;
        private StorageFile selectedStorageFile;
        private string result = "";
        private float resultProbability = 0;
        public MainPage()
        {
            this.InitializeComponent();
            loadModel();
            this.OnUpdatedWeather += MainPage_OnUpdatedWeather;

        }

        private void MainPage_OnUpdatedWeather()
        {
            Debug.Print("New Weather Value Detected");
        }

        private async Task loadModel()
        {
            // Get an access the ONNX model and save it in memory.
            StorageFile modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/classifier.onnx"));
            // Instantiate the model. 
            modelGen = await classifierModel.CreateFromStreamAsync(modelFile);
        }
        // Waiting for a click event to select a file 
        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await getImage())
            {
                return;
            }
            // After the click event happened and an input selected, begin the model execution. 
            // Bind the model input
            await imageBind();
            // Model evaluation
            await evaluate();
            // Extract the results
            extractResult();
            // Display the results  
            await displayResult();
        }
        // A method to select an input image file
        private async Task<bool> getImage()
        {
            try
            {
                // Trigger file picker to select an image file
                FileOpenPicker fileOpenPicker = new FileOpenPicker();
                fileOpenPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                fileOpenPicker.FileTypeFilter.Add(".jpg");
                fileOpenPicker.FileTypeFilter.Add(".png");
                fileOpenPicker.ViewMode = PickerViewMode.Thumbnail;
                selectedStorageFile = await fileOpenPicker.PickSingleFileAsync();
                if (selectedStorageFile == null)
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        // A method to convert and bind the input image.  
        private async Task imageBind()
        {
            UIPreviewImage.Source = null;
            try
            {
                SoftwareBitmap softwareBitmap;
                using (IRandomAccessStream stream = await selectedStorageFile.OpenAsync(FileAccessMode.Read))
                {
                    // Create the decoder from the stream 
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                    // Get the SoftwareBitmap representation of the file in BGRA8 format
                    softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }
                // Display the image
                SoftwareBitmapSource imageSource = new SoftwareBitmapSource();
                await imageSource.SetBitmapAsync(softwareBitmap);
                UIPreviewImage.Source = imageSource;
                // Encapsulate the image within a VideoFrame to be bound and evaluated
                VideoFrame inputImage = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap);
                // bind the input image
                ImageFeatureValue imageTensor = ImageFeatureValue.CreateFromVideoFrame(inputImage);
                input.input_2 = imageTensor;
            }
            catch (Exception e)
            {
            }
        
        }
        // A method to evaluate the model
        private async Task evaluate()
        {
            output = await modelGen.EvaluateAsync(input);
        }
        
        // A method to extract output (string and a probability) from the "loss" output of the model 
        private void extractResult()
        {
            var tsResult = output.dense_5;
            var vectorResult = tsResult.GetAsVectorView();

            float maxProbability = 0;
            int keyOfMax = 0;

            for (int i = 0; i < vectorResult.Count; i++)
            {
                var elementProbability = vectorResult[i];
                if (elementProbability > maxProbability)
                {
                    maxProbability = elementProbability;
                    keyOfMax = i;
                }
            }
            List<string> Labels = new List<string>() { "sunrise","cloudy","shine","rain"};
            result = Labels[keyOfMax] ;
            resultProbability = maxProbability;
            OnUpdatedWeather?.Invoke();
        }

        // A method to display the results
        private async Task displayResult()
        {
            displayOutput.Text = result.ToString();
            displayProbability.Text = resultProbability.ToString();
        }
        private event Action OnUpdatedWeather;

    }
}
