export const sendHttpPOSTCall = 
  async (
  url: string,
  body: string
): Promise<Response> => 
{
  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: body 
  });

  return response;     
}

export const sendHttpGet = 
  async (
    url: string
): Promise<Response> => 
{
  const response = await fetch(url);
  return response;
}

